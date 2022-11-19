namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectTableJoinedNode : ExpressionNode
{
    public ExpressionNode RightTableNode { get; }

    public ExpressionNode SearchConditionNode { get; }

    public SelectTableJoinedTypeNode JoinTypeNode { get; }

    /// <inheritdoc />
    public override string Code => "tablejoin";

    public SelectTableJoinedNode(
        ExpressionNode rightTableNode,
        SelectTableJoinedTypeNode joinTypeNode,
        ExpressionNode searchConditionNode)
    {
        RightTableNode = rightTableNode;
        JoinTypeNode = joinTypeNode;
        SearchConditionNode = searchConditionNode;
    }

    public SelectTableJoinedNode(SelectTableJoinedNode node) : this(
        (ExpressionNode)node.RightTableNode.Clone(),
        (SelectTableJoinedTypeNode)node.JoinTypeNode.Clone(),
        (ExpressionNode)node.SearchConditionNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectTableJoinedNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return RightTableNode;
        yield return SearchConditionNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
