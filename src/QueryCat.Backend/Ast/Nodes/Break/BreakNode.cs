namespace QueryCat.Backend.Ast.Nodes.Break;

internal sealed class BreakNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "break";

    public BreakNode()
    {
    }

    public BreakNode(BreakNode node)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new BreakNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
