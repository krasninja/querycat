namespace QueryCat.Backend.Ast.Nodes;

internal sealed class BlockExpressionNode : ExpressionNode
{
    public List<StatementNode> Statements { get; } = new();

    /// <inheritdoc />
    public override string Code => "block";

    public BlockExpressionNode(IList<StatementNode> statements)
    {
        if (statements.Count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(statements), Resources.Errors.NoStatements);
        }
        Statements.AddRange(statements);
    }

    public BlockExpressionNode(BlockExpressionNode node)
        : this(node.Statements.Select(s => (StatementNode)s.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var statementNode in Statements)
        {
            yield return statementNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new BlockExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
