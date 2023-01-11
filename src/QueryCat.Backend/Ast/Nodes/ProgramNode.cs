namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Root node, program entry point. Program consist of the list of
/// statements.
/// </summary>
public sealed class ProgramNode : AstNode
{
    public List<StatementNode> Statements { get; } = new();

    /// <inheritdoc />
    public override string Code => "program";

    public ProgramNode(IList<StatementNode> statements)
    {
        Statements.AddRange(statements);
    }

    public ProgramNode(ProgramNode node) : this(
        node.Statements.Select(s => (StatementNode)s.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => Statements;

    /// <inheritdoc />
    public override object Clone() => new ProgramNode(this);

    /// <inheritdoc />
    public override string ToString() => $"program: {Statements.Count} statement(-s)";
}
