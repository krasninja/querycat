namespace QueryCat.Backend.Ast.Nodes.Select;

public abstract class SelectTableJoinedNode : ExpressionNode
{
    public ExpressionNode RightTableNode { get; }

    public SelectTableJoinedTypeNode JoinTypeNode { get; }

    public SelectTableJoinedNode(
        ExpressionNode rightTableNode,
        SelectTableJoinedTypeNode joinTypeNode)
    {
        RightTableNode = rightTableNode;
        JoinTypeNode = joinTypeNode;
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return RightTableNode;
        yield return JoinTypeNode;
    }
}
