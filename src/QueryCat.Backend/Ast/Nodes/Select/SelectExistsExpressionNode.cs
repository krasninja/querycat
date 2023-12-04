namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectExistsExpressionNode : ExpressionNode
{
    public SelectQueryNode SubQueryNode { get; }

    public SelectExistsExpressionNode(SelectQueryNode subQueryNode)
    {
        SubQueryNode = subQueryNode;
    }

    public SelectExistsExpressionNode(SelectExistsExpressionNode node) :
        this((SelectQueryNode)node.SubQueryNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override string Code => "exists";

    /// <inheritdoc />
    public override object Clone() => new SelectExistsExpressionNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return SubQueryNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => $"Exists ({SubQueryNode})";
}
