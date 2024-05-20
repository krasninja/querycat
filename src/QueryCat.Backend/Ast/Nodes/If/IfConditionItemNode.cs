namespace QueryCat.Backend.Ast.Nodes.If;

internal sealed class IfConditionItemNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "if_item";

    public ExpressionNode ConditionNode { get; }

    public BlockExpressionNode BlockExpressionNode { get; }

    public IfConditionItemNode(ExpressionNode conditionNode, BlockExpressionNode blockExpressionNode)
    {
        ConditionNode = conditionNode;
        BlockExpressionNode = blockExpressionNode;
    }

    public IfConditionItemNode(IfConditionItemNode node)
        : this((ExpressionNode)node.ConditionNode.Clone(), (BlockExpressionNode)node.BlockExpressionNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ConditionNode;
        yield return BlockExpressionNode;
    }

    /// <inheritdoc />
    public override object Clone() => new IfConditionItemNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
