namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectFetchNode : AstNode
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
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => $"{CountNode}";
}
