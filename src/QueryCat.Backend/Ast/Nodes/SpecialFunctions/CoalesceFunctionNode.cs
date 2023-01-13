namespace QueryCat.Backend.Ast.Nodes.SpecialFunctions;

public sealed class CoalesceFunctionNode : ExpressionNode
{
    public List<ExpressionNode> Expressions { get; } = new();

    /// <inheritdoc />
    public override string Code => "coalesce";

    public CoalesceFunctionNode(IEnumerable<ExpressionNode> expressions)
    {
        Expressions.AddRange(expressions);
    }

    public CoalesceFunctionNode(CoalesceFunctionNode node) : this(
        node.Expressions.Select(e => (ExpressionNode)e.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new CoalesceFunctionNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => Expressions;

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
