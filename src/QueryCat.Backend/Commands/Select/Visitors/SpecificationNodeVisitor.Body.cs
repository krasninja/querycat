using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal partial class SpecificationNodeVisitor
{
    /// <inheritdoc />
    public override void Visit(SelectQueryExpressionBodyNode node)
    {
        // Create compound context.
        var isSubQuery = _parentSpecificationNode != null;
        var firstQueryContext = node.QueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var context = new SelectCommandContext(firstQueryContext.CurrentIterator)
        {
            RowsInputIterator = firstQueryContext.RowsInputIterator,
        };
        context.AddChildContext(firstQueryContext.ChildContexts);

        // Process.
        ApplyRowIdIterator(context, isSubQuery);
        ApplyOrderBy(context, node.OrderByNode);
        ApplyOffsetFetch(context, node.OffsetNode, node.FetchNode);
        var resultIterator = context.CurrentIterator;
        if (context.HasOutput)
        {
            resultIterator = new ExecuteRowsIterator(resultIterator);
        }

        // Set result. If INTO clause is specified we do not return IRowsIterator outside. Just
        // iterating it we will save rows into target. Otherwise we return it as is.
        node.SetAttribute(AstAttributeKeys.ResultKey, resultIterator);
        node.SetAttribute(AstAttributeKeys.ContextKey, context);
    }

    private void ApplyRowIdIterator(SelectCommandContext bodyContext, bool isSubQuery)
    {
        var resultIterator = bodyContext.CurrentIterator;
        if (ExecutionThread.Options.AddRowNumberColumn && !isSubQuery)
        {
            resultIterator = new RowIdRowsIterator(resultIterator);
        }
        bodyContext.SetIterator(resultIterator);
    }
}
