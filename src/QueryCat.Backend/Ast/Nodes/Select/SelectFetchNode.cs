namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectFetchNode : AstNode
{
    public ExpressionNode CountNode { get; }

    /// <inheritdoc />
    public override string Code => "fetch";

    public SelectFetchNode(ExpressionNode countNode)
    {
        CountNode = countNode;
    }

    public SelectFetchNode(SelectFetchNode node) : this((ExpressionNode)node.CountNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return CountNode;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectFetchNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => $"{CountNode}";
}
