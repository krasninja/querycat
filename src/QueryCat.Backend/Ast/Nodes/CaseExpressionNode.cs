namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Represents SQL CASE clause. There are two cases:
/// - Search case: CASE WHEN a=1 THEN 1 WHEN a=2 THEN 2 ELSE 3 END;
/// - Simple form: CASE a WHEN 1 THEN 1 WHEN 2 THEN 2 ELSE 3 END;.
/// </summary>
internal sealed class CaseExpressionNode : ExpressionNode
{
    public ExpressionNode? ArgumentNode { get; }

    public List<CaseWhenThenNode> WhenNodes { get; } = new();

    public ExpressionNode? DefaultNode { get; }

    public bool IsSearchCase => ArgumentNode == null;

    public bool IsSimpleCase => ArgumentNode != null;

    /// <inheritdoc />
    public override string Code => "case_expr";

    public CaseExpressionNode(
        ExpressionNode? argumentNode,
        IEnumerable<CaseWhenThenNode> when,
        ExpressionNode? @default = null)
    {
        ArgumentNode = argumentNode;
        WhenNodes.AddRange(when);
        DefaultNode = @default;
    }

    public CaseExpressionNode(
        IEnumerable<CaseWhenThenNode> when,
        ExpressionNode? @default = null) : this(null, when, @default)
    {
    }

    public CaseExpressionNode(CaseExpressionNode node)
        : this(
            (ExpressionNode?)node.ArgumentNode?.Clone(),
            node.WhenNodes.Select(n => (CaseWhenThenNode)n.Clone()).ToList(),
            (ExpressionNode?)node.DefaultNode?.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new CaseExpressionNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        if (ArgumentNode != null)
        {
            yield return ArgumentNode;
        }
        foreach (var whenNode in WhenNodes)
        {
            yield return whenNode;
        }
        if (DefaultNode != null)
        {
            yield return DefaultNode;
        }
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
