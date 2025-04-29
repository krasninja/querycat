namespace QueryCat.Backend.Ast.Nodes.Continue;

internal sealed class ContinueNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "continue";

    public ContinueNode()
    {
    }

    public ContinueNode(ContinueNode node)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new ContinueNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
