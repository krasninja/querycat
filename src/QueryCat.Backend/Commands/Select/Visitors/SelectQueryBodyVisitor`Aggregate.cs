using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed partial class SelectQueryBodyVisitor
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

    private void ApplyAggregate(
        SelectCommandContext context,
        SelectQuerySpecificationNode selectQueryNode)
    {
        var groupByNode = selectQueryNode.TableExpression?.GroupByNode;
        var targets = CreateAggregateTargets(context, selectQueryNode.TableExpression, selectQueryNode.ColumnsList);

        // If there is no group by and no aggregate functions used - skip aggregates
        // processing.
        if (groupByNode == null && targets.Length < 1)
        {
            return;
        }

        var keysFactory = CreateGroupKeyValuesFactory(groupByNode, context.CurrentIterator, context);
        var aggregateColumnsOffset = context.CurrentIterator.Columns.Length;

        context.AppendIterator(
            new GroupRowsIterator(context.CurrentIterator, keysFactory, context, targets));

        ReplaceAggregateFunctionsByColumnReference(selectQueryNode, targets, aggregateColumnsOffset);
    }

    private void ReplaceAggregateFunctionsByColumnReference(
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
                    Logger.Instance.Warning("Cannot find node!");
                    return;
                }

                var targetIndex = index + aggregateColumnsOffset;
                functionCallNode.SetAttribute(AstAttributeKeys.InputAggregateIndexKey, targetIndex);
            }
        };

        var havingNode = selectQueryNode.TableExpression?.HavingNode;
        if (havingNode != null)
        {
            aggregateReplaceDelegateVisitor.Run(havingNode);
        }
        aggregateReplaceDelegateVisitor.Run(selectQueryNode.ColumnsList);
    }

    private AggregateTarget[] CreateAggregateTargets(
        SelectCommandContext context,
        SelectTableExpressionNode? tableExpressionNode,
        SelectColumnsListNode columnsNodes)
    {
        var havingNode = tableExpressionNode?.HavingNode;
        var selectAggregateTargetsVisitor = new SelectMakeDelegateVisitor(_executionThread, context);

        selectAggregateTargetsVisitor.Run(columnsNodes);
        var aggregateTargets = columnsNodes.GetAllChildren<FunctionCallNode>()
            .Select(n => n.GetAttribute<AggregateTarget>(AstAttributeKeys.AggregateFunctionKey));
        if (havingNode != null)
        {
            selectAggregateTargetsVisitor.Run(havingNode);
            aggregateTargets = aggregateTargets.Union(
                havingNode.GetAllChildren<FunctionCallNode>()
                    .Select(n => n.GetAttribute<AggregateTarget>(AstAttributeKeys.AggregateFunctionKey)));
        }
        return aggregateTargets.OfType<AggregateTarget>().ToArray();
    }

    private void ApplyHaving(
        SelectCommandContext context,
        SelectHavingNode? havingNode)
    {
        if (havingNode == null)
        {
            return;
        }

        var predicate = new SelectMakeDelegateVisitor(_executionThread, context).RunAndReturn(havingNode);
        context.CurrentIterator = new FilterRowsIterator(context.CurrentIterator, predicate);
    }

    private FuncUnit[] CreateGroupKeyValuesFactory(SelectGroupByNode? groupByNode, IRowsIterator rowsIterator,
        SelectCommandContext context)
    {
        // If there is no GROUP BY statement but there are aggregates functions in SELECT -
        // just generate "fake" key.
        if (groupByNode == null || !groupByNode.GroupBy.Any())
        {
            return new FuncUnit[]
            {
                new(VariantValue.OneIntegerValue)
            };
        }
        var makeDelegateVisitor = new SelectMakeDelegateVisitor(_executionThread, context);
        return groupByNode.GroupBy.Select(n => makeDelegateVisitor.RunAndReturn(n)).ToArray();
    }
}
