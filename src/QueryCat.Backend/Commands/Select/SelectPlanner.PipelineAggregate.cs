using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Commands.Select.Visitors;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    /*
     * For aggregate queries we break pipeline execution and have to prepare new rows frame.
     * We also prepare new columns. For example:
     *
     * Table: id, first, last, balance
     * SELECT sum(balance) FROM tbl GROUP BY first HAVING count(*) > 2;
     *
     * Final aggregate rows frame columns:
     * id, first, last, balance, sum(balance), count(*)
     *
     * 0-3 - the columns copy from input table
     * 4-5 - calculated aggregates
     * aggregateColumnsOffset = 4
     */

    private async Task PipelineAggregate_ApplyGroupingAsync(
        SelectCommandContext context,
        SelectQuerySpecificationNode selectQueryNode,
        CancellationToken cancellationToken)
    {
        var groupByNode = selectQueryNode.TableExpressionNode?.GroupByNode;
        var targets = await PipelineAggregate_CreateTargetsAsync(context,
            selectQueryNode.TableExpressionNode, selectQueryNode.ColumnsListNode, cancellationToken);

        // If there is no group by and no aggregate functions used - skip aggregates
        // processing.
        if (groupByNode == null && targets.Length < 1)
        {
            return;
        }

        var keysFactory = PipelineAggregate_CreateGroupKeyValuesFactory(groupByNode, context);
        var aggregateColumnsOffset = context.CurrentIterator.Columns.Length;

        context.SetIterator(
            new GroupRowsIterator(ExecutionThread, context.CurrentIterator, keysFactory, context, targets));

        PipelineAggregate_ReplaceAggregateFunctionsByColumnReference(selectQueryNode, targets, aggregateColumnsOffset);
    }

    private static void PipelineAggregate_ReplaceAggregateFunctionsByColumnReference(
        SelectQuerySpecificationNode selectQueryNode,
        AggregateTarget[] targets,
        int aggregateColumnsOffset)
    {
        var aggregateReplaceDelegateVisitor = new CallbackDelegateVisitor
        {
            Callback = (node, _) =>
            {
                if (node is not FunctionCallNode functionCallNode)
                {
                    return;
                }

                var index = Array.FindIndex(targets, t => t.Node.Id == node.Id);
                if (index < 0)
                {
                    return;
                }

                var targetIndex = index + aggregateColumnsOffset;
                functionCallNode.SetAttribute(AstAttributeKeys.InputAggregateIndexKey, targetIndex);
            }
        };

        var havingNode = selectQueryNode.TableExpressionNode?.HavingNode;
        if (havingNode != null)
        {
            aggregateReplaceDelegateVisitor.Run(havingNode);
        }
        aggregateReplaceDelegateVisitor.Run(selectQueryNode.ColumnsListNode);
    }

    private async Task<AggregateTarget[]> PipelineAggregate_CreateTargetsAsync(
        SelectCommandContext context,
        SelectTableNode? tableExpressionNode,
        SelectColumnsListNode columnsNodes,
        CancellationToken cancellationToken)
    {
        var havingNode = tableExpressionNode?.HavingNode;

        var columnsWithFunctions = columnsNodes.ColumnsNodes
            .OfType<SelectColumnsSublistExpressionNode>()
            .SelectMany(n => n.GetAllChildren<FunctionCallNode>([typeof(SelectQueryNode)]))
            .ToList();

        var selectAggregateTargetsVisitor = new SelectCreateDelegateVisitor(ExecutionThread, context);
        await selectAggregateTargetsVisitor.RunAsync(columnsWithFunctions, cancellationToken);
        var aggregateTargets = columnsWithFunctions
            .Select(n => n.GetAttribute<AggregateTarget>(AstAttributeKeys.AggregateFunctionKey));
        if (havingNode != null)
        {
            await selectAggregateTargetsVisitor.RunAsync(havingNode, cancellationToken);
            aggregateTargets = aggregateTargets.Union(
                havingNode.GetAllChildren<FunctionCallNode>()
                    .Select(n => n.GetAttribute<AggregateTarget>(AstAttributeKeys.AggregateFunctionKey)));
        }
        return aggregateTargets.OfType<AggregateTarget>().ToArray();
    }

    private void PipelineAggregate_ApplyHaving(
        SelectCommandContext context,
        SelectHavingNode? havingNode)
    {
        if (havingNode == null)
        {
            return;
        }

        var predicate = Misc_CreateDelegate(havingNode, context);
        context.SetIterator(new FilterRowsIterator(ExecutionThread, context.CurrentIterator, predicate));
    }

    private IFuncUnit[] PipelineAggregate_CreateGroupKeyValuesFactory(SelectGroupByNode? groupByNode,
        SelectCommandContext context)
    {
        // If there is no GROUP BY statement but there are aggregates functions in SELECT -
        // just generate "fake" special key.
        if (groupByNode == null || !groupByNode.GroupByNodes.Any())
        {
            return GroupRowsIterator.NoGroupsKeyFactory;
        }
        return Misc_CreateDelegate(groupByNode.GroupByNodes, context).ToArray();
    }
}
