using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// The visitor is po process <see cref="SelectQueryExpressionBodyNode" /> nodes only in post order way.
/// </summary>
internal sealed class SelectBodyNodeVisitor : SelectAstVisitor
{
    public SelectBodyNodeVisitor(ExecutionThread executionThread) : base(executionThread)
    {
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        AstTraversal.PostOrder(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectQueryExpressionBodyNode node)
    {
        var selectQueryBodyVisitor = new SelectSpecificationNodeVisitor(ExecutionThread);
        selectQueryBodyVisitor.Run(node.Queries);

        // Add all iterators and merge them into "combine" iterator.
        var combineRowsIterator = new CombineRowsIterator();
        var hasOutputInQuery = false;
        foreach (var queryNode in node.Queries)
        {
            var queryContext = queryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
            if (queryContext.HasOutput)
            {
                hasOutputInQuery = true;
            }
            combineRowsIterator.AddRowsIterator(queryContext.CurrentIterator);
        }

        // Format final result iterator.
        var resultIterator = combineRowsIterator.RowIterators.Count == 1
            ? combineRowsIterator.RowIterators.First()
            : combineRowsIterator;
        if (ExecutionThread.Options.AddRowNumberColumn)
        {
            resultIterator = new RowIdRowsIterator(resultIterator);
        }

        var firstQueryContext = node.Queries[0].GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var context = new SelectCommandContext(resultIterator)
        {
            RowsInputIterator = firstQueryContext.RowsInputIterator
        };
        ApplyOrderBy(context, node.OrderBy);
        ApplyOffsetFetch(context, node.Offset, node.Fetch);
        CreateSelectRowsSet(context, node.Queries[0]);
        resultIterator = context.CurrentIterator;

        // Set result. If INTO clause is specified we do not return IRowsIterator outside. Just
        // iterating it we will save rows into target. Otherwise we return it as is.
        node.SetAttribute(AstAttributeKeys.ResultKey, resultIterator);
        if (hasOutputInQuery)
        {
            node.SetFunc(() =>
            {
                resultIterator.MoveToEnd();
                return VariantValue.Null;
            });
        }
        else
        {
            node.SetFunc(() => VariantValue.CreateFromObject(resultIterator));
        }
    }

    #region SELECT

    private void CreateSelectRowsSet(
        SelectCommandContext context,
        SelectQuerySpecificationNode querySpecificationNode)
    {
        var iterator = context.CurrentIterator;
        var projectedIterator = new ProjectedRowsIterator(iterator);
        var firstQueryContext = querySpecificationNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        for (int i = 0; i < firstQueryContext.CurrentIterator.Columns.Length; i++)
        {
            var columnIndex = i;
            projectedIterator.AddFuncColumn(firstQueryContext.CurrentIterator.Columns[i],
                new FuncUnit(data => iterator.Current[columnIndex]));
        }
        context.AppendIterator(projectedIterator);
    }

    #endregion
}
