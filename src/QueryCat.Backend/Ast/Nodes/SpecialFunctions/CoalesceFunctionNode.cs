namespace QueryCat.Backend.Ast.Nodes.SpecialFunctions;

public sealed class CoalesceFunctionNode : ExpressionNode
{
    public IList<ExpressionNode> Expressions { get; }

    /// <inheritdoc />
    public override string Code => "coalesce";

    public CoalesceFunctionNode(IList<ExpressionNode> expressions)
    {
        Expressions = expressions;
    }

    public CoalesceFunctionNode(CoalesceFunctionNode node) : this(
        node.Expressions.Select(e => (ExpressionNode)e.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new CoalesceFunctionNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        return Expressions;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
