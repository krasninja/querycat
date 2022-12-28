using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal partial class SetIteratorVisitor
{
    #region UNION

    /// <inheritdoc />
    public override void Visit(SelectQueryCombineNode node)
    {
        var context = node.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var leftContext = node.LeftQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var rightContext = node.RightQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);
        var combineRowsIterator = new CombineRowsIterator(
            leftContext.CurrentIterator,
            rightContext.CurrentIterator,
            ConvertCombineType(node.CombineType),
            node.IsDistinct);
        context.RowsInputIterator = leftContext.RowsInputIterator;
        context.SetIterator(combineRowsIterator);

        // Process.
        ApplyRowIdIterator(context, context.Parent != null);
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

    private static CombineType ConvertCombineType(SelectQueryCombineType combineType) => combineType switch
    {
        SelectQueryCombineType.Except => CombineType.Except,
        SelectQueryCombineType.Intersect => CombineType.Intersect,
        SelectQueryCombineType.Union => CombineType.Union,
        _ => throw new NotImplementedException($"{combineType} is not implemented."),
    };

    #endregion
}
