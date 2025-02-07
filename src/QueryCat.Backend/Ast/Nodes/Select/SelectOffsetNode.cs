namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectOffsetNode : AstNode
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
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return CountNode;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectOffsetNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => CountNode.ToString() ?? string.Empty;
}
