namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectOrderBySpecificationNode : AstNode
{
    public ExpressionNode ExpressionNode { get; }

    public SelectOrderSpecification Order { get; }

    public SelectNullOrder NullOrder { get; }

    /// <inheritdoc />
    public override string Code => "sort_spec";

    public SelectOrderBySpecificationNode(
        ExpressionNode expressionNode,
        SelectOrderSpecification order = SelectOrderSpecification.Ascending,
        SelectNullOrder nullOrder = SelectNullOrder.NullsLast)
    {
        ExpressionNode = expressionNode;
        Order = order;
        NullOrder = nullOrder;
    }

    public SelectOrderBySpecificationNode(SelectOrderBySpecificationNode node)
        : this((ExpressionNode)node.ExpressionNode.Clone(), node.Order, node.NullOrder)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ExpressionNode;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectOrderBySpecificationNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
