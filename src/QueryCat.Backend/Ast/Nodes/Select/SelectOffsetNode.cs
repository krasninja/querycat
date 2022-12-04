namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectOffsetNode : AstNode
{
    public ExpressionNode CountNode { get; }

    /// <inheritdoc />
    public override string Code => "offset";

    public SelectOffsetNode(ExpressionNode countNode)
    {
        CountNode = countNode;
    }

    public SelectOffsetNode(SelectOffsetNode node) : this((ExpressionNode)node.CountNode.Clone())
    {
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return CountNode;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectOffsetNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => CountNode.ToString() ?? string.Empty;
}
