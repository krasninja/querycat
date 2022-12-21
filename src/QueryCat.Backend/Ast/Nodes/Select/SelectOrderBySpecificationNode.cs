namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectOrderBySpecificationNode : AstNode
{
    public ExpressionNode Expression { get; }

    public SelectOrderSpecification Order { get; }

    public SelectNullOrdering NullOrder { get; }

    /// <inheritdoc />
    public override string Code => "sort_spec";

    public SelectOrderBySpecificationNode(
        ExpressionNode expression,
        SelectOrderSpecification order = SelectOrderSpecification.Ascending,
        SelectNullOrdering nullOrder = SelectNullOrdering.NullsLast)
    {
        Expression = expression;
        Order = order;
        NullOrder = nullOrder;
    }

    public SelectOrderBySpecificationNode(SelectOrderBySpecificationNode node)
        : this((ExpressionNode)node.Expression.Clone(), node.Order, node.NullOrder)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return Expression;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectOrderBySpecificationNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
