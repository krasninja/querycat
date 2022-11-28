namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Represents SQL CASE clause. There are two cases:
/// - Search case: CASE WHEN a=1 THEN 1 WHEN a=2 THEN 2 ELSE 3 END;
/// - Simple form: CASE a WHEN 1 THEN 1 WHEN 2 THEN 2 ELSE 3 END;.
/// </summary>
public sealed class CaseExpressionNode : ExpressionNode
{
    public ExpressionNode? Argument { get; }

    public List<CaseWhenThenNode> WhenNodes { get; }

    public ExpressionNode? DefaultNode { get; }

    public bool IsSearchCase => Argument == null;

    public bool IsSimpleCase => Argument != null;

    /// <inheritdoc />
    public override string Code => "case_expr";

    public CaseExpressionNode(
        ExpressionNode? argument,
        List<CaseWhenThenNode> when,
        ExpressionNode? @default = null)
    {
        Argument = argument;
        WhenNodes = when;
        DefaultNode = @default;
    }

    public CaseExpressionNode(
        List<CaseWhenThenNode> when,
        ExpressionNode? @default = null) : this(null, when, @default)
    {
    }

    public CaseExpressionNode(CaseExpressionNode node)
        : this(
            (ExpressionNode?)node.Argument?.Clone(),
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
        if (Argument != null)
        {
            yield return Argument;
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
}
