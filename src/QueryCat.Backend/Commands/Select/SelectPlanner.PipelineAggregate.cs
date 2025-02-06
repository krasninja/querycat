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

        var keysFactory = await PipelineAggregate_CreateGroupKeyValuesFactoryAsync(groupByNode, context, cancellationToken);
        var aggregateColumnsOffset = context.CurrentIterator.Columns.Length;

        context.SetIterator(
            new GroupRowsIterator(ExecutionThread, context.CurrentIterator, keysFactory, context, targets));

        await PipelineAggregate_ReplaceAggregateFunctionsByColumnReferenceAsync(selectQueryNode,
            targets, aggregateColumnsOffset, cancellationToken);
    }

    private static async Task PipelineAggregate_ReplaceAggregateFunctionsByColumnReferenceAsync(
        SelectQuerySpecificationNode selectQueryNode,
        AggregateTarget[] targets,
        int aggregateColumnsOffset,
        CancellationToken cancellationToken)
    {
        var aggregateReplaceDelegateVisitor = new CallbackDelegateVisitor
        {
            Callback = (node, _, _) =>
            {
                if (node is not FunctionCallNode functionCallNode)
                {
                    return ValueTask.CompletedTask;
                }

                var index = Array.FindIndex(targets, t => t.Node.Id == node.Id);
                if (index < 0)
                {
                    return ValueTask.CompletedTask;
                }

                var targetIndex = index + aggregateColumnsOffset;
                functionCallNode.SetAttribute(AstAttributeKeys.InputAggregateIndexKey, targetIndex);
                return ValueTask.CompletedTask;
            }
        };

        var havingNode = selectQueryNode.TableExpressionNode?.HavingNode;
        if (havingNode != null)
        {
            await aggregateReplaceDelegateVisitor.RunAsync(havingNode, cancellationToken);
        }
        await aggregateReplaceDelegateVisitor.RunAsync(selectQueryNode.ColumnsListNode, cancellationToken);
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

    private async Task PipelineAggregate_ApplyHavingAsync(
        SelectCommandContext context,
        SelectHavingNode? havingNode,
        CancellationToken cancellationToken)
    {
        if (havingNode == null)
        {
            return;
        }

        var predicate = await Misc_CreateDelegateAsync(havingNode, context, cancellationToken);
        context.SetIterator(new FilterRowsIterator(ExecutionThread, context.CurrentIterator, predicate));
    }

    private async Task<IFuncUnit[]> PipelineAggregate_CreateGroupKeyValuesFactoryAsync(SelectGroupByNode? groupByNode,
        SelectCommandContext context, CancellationToken cancellationToken)
    {
        // If there is no GROUP BY statement but there are aggregates functions in SELECT -
        // just generate "fake" special key.
        if (groupByNode == null || !groupByNode.GroupByNodes.Any())
        {
            return GroupRowsIterator.NoGroupsKeyFactory;
        }
        return (await Misc_CreateDelegateAsync(groupByNode.GroupByNodes, context, cancellationToken))
            .ToArray();
    }
}
