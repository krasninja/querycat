using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Iterators;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal abstract class SelectAstVisitor : AstVisitor
{
    protected AstTraversal AstTraversal { get; }

    protected ExecutionThread ExecutionThread { get; }

    public SelectAstVisitor(ExecutionThread executionThread)
    {
        ExecutionThread = executionThread;
        AstTraversal = new AstTraversal(this);
    }

    #region ORDER BY

    protected void ApplyOrderBy(SelectCommandContext context, SelectOrderByNode? orderByNode)
    {
        if (orderByNode == null)
        {
            return;
        }
        new SelectResolveTypesVisitor(ExecutionThread, context).Run(orderByNode);

        // Create wrapper to initialize rows frame and create index.
        var makeDelegateVisitor = new SelectCreateDelegateVisitor(ExecutionThread, context);
        var orderFunctions = orderByNode.OrderBySpecificationNodes.Select(n =>
            new OrderRowsIterator.OrderBy(
                makeDelegateVisitor.RunAndReturn(n.Expression),
                ConvertDirection(n.Order),
                n.GetDataType()
            )
        );
        var scope = new VariantValueFuncData(context.CurrentIterator);
        context.AppendIterator(new OrderRowsIterator(scope, orderFunctions.ToArray()));
    }

    protected OrderDirection ConvertDirection(SelectOrderSpecification order) => order switch
    {
        SelectOrderSpecification.Ascending => OrderDirection.Ascending,
        SelectOrderSpecification.Descending => OrderDirection.Descending,
        _ => throw new ArgumentOutOfRangeException(nameof(order)),
    };

    #endregion

    #region OFFSET, FETCH

    protected void ApplyOffsetFetch(
        SelectCommandContext context,
        SelectOffsetNode? offsetNode,
        SelectFetchNode? fetchNode)
    {
        var makeDelegateVisitor = new SelectCreateDelegateVisitor(ExecutionThread, context);
        if (offsetNode != null)
        {
            var count = makeDelegateVisitor.RunAndReturn(offsetNode.CountNode).Invoke().AsInteger;
            context.CurrentIterator = new OffsetRowsIterator(context.CurrentIterator, count);
        }
        if (fetchNode != null)
        {
            var count = makeDelegateVisitor.RunAndReturn(fetchNode.CountNode).Invoke().AsInteger;
            context.CurrentIterator = new LimitRowsIterator(context.CurrentIterator, count);
        }
    }

    #endregion
}
