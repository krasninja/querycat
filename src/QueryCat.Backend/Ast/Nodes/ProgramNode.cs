namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Root node, program entry point. Program consist of the list of
/// statements.
/// </summary>
internal sealed class ProgramNode : AstNode
{
    public List<StatementNode> Statements { get; } = new();

    /// <inheritdoc />
    public override string Code => "program";

    public ProgramNode(params IList<StatementNode> statements)
    {
        Statements.AddRange(statements);
    }

    public ProgramNode(ProgramNode node) : this(
        node.Statements.Select(s => (StatementNode)s.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => Statements;

    /// <inheritdoc />
    public override object Clone() => new ProgramNode(this);

    /// <inheritdoc />
    public override string ToString() => $"program: {Statements.Count} statement(-s)";
}
