namespace QueryCat.Backend.Ast.Nodes;

internal sealed class ProgramBodyNode : AstNode
{
    public List<StatementNode> Statements { get; } = new();

    /// <inheritdoc />
    public override string Code => "program_body";

    public ProgramBodyNode(params IList<StatementNode> statements)
    {
        Statements.AddRange(statements);
    }

    public ProgramBodyNode(ProgramBodyNode node) : this(
        node.Statements.Select(s => (StatementNode)s.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => Statements;

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override object Clone() => new ProgramBodyNode(this);

    /// <inheritdoc />
    public override string ToString() => $"program: {Statements.Count} statement(-s)";
}
