using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal partial class SpecificationNodeVisitor
{
    /// <inheritdoc />
    public override void Visit(SelectQueryExpressionBodyNode node)
    {
        // Add all iterators and merge them into "combine" iterator.
        var combineRowsIterator = new CombineRowsIterator();
        var hasOutputInQuery = false;

        // Create compound context.
        var isSubQuery = _parentSpecificationNode != null;
        var firstQueryContext = node.Queries[0].GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var context = new SelectCommandContext(combineRowsIterator)
        {
            RowsInputIterator = firstQueryContext.RowsInputIterator,
        };
        context.AddChildContext(firstQueryContext.ChildContexts);

        // Process.
        CreateCombineRowsSet(context, node, isSubQuery, ref hasOutputInQuery);
        ApplyOrderBy(context, node.OrderByNode);
        ApplyOffsetFetch(context, node.OffsetNode, node.FetchNode);
        var resultIterator = context.CurrentIterator;
        if (hasOutputInQuery)
        {
            resultIterator = new ExecuteRowsIterator(resultIterator);
        }

        // Set result. If INTO clause is specified we do not return IRowsIterator outside. Just
        // iterating it we will save rows into target. Otherwise we return it as is.
        node.SetAttribute(AstAttributeKeys.ResultKey, resultIterator);
        node.SetAttribute(AstAttributeKeys.ContextKey, context);
    }

    private void CreateCombineRowsSet(
        SelectCommandContext context,
        SelectQueryExpressionBodyNode node,
        bool isSubQuery,
        ref bool hasOutputInQuery)
    {
        // Add all iterators and merge them into "combine" iterator.
        var combineRowsIterator = new CombineRowsIterator();
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
        if (ExecutionThread.Options.AddRowNumberColumn && !isSubQuery)
        {
            resultIterator = new RowIdRowsIterator(resultIterator);
        }

        context.SetIterator(resultIterator);
    }
}
