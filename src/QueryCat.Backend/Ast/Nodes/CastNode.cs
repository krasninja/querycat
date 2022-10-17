namespace QueryCat.Backend.Ast.Nodes;

public sealed class CastNode : ExpressionNode
{
    public ExpressionNode ExpressionNode { get; }

    public TypeNode TargetTypeNode { get; }

    /// <inheritdoc />
    public override string Code => "cast";

    /// <inheritdoc />
    public CastNode(ExpressionNode expressionNode, TypeNode targetTypeNode)
    {
        ExpressionNode = expressionNode;
        TargetTypeNode = targetTypeNode;
    }

    public CastNode(CastNode node) : this(
        (ExpressionNode)node.ExpressionNode.Clone(), (TypeNode)node.TargetTypeNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ExpressionNode;
    }

    /// <inheritdoc />
    public override object Clone() => new CastNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
