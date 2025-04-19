namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Root node, program entry point. Program consist of the list of
/// statements.
/// </summary>
internal sealed class ProgramNode : AstNode
{
    public ProgramBodyNode Body { get; }

    /// <inheritdoc />
    public override string Code => "program";

    public ProgramNode(ProgramBodyNode bodyNode)
    {
        Body = bodyNode;
    }

    public ProgramNode(ProgramNode node) : this((ProgramBodyNode)node.Body.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return Body;
    }

    /// <inheritdoc />
    public override object Clone() => new ProgramNode(this);
}
