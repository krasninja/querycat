namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectTableJoinedOnNode : SelectTableJoinedNode
{
    public ExpressionNode SearchConditionNode { get; }

    /// <inheritdoc />
    public override string Code => "tablejoinon";

    public SelectTableJoinedOnNode(
        ExpressionNode rightTableNode,
        SelectTableJoinedTypeNode joinTypeNode,
        ExpressionNode searchConditionNode) : base(rightTableNode, joinTypeNode)
    {
        SearchConditionNode = searchConditionNode;
    }

    public SelectTableJoinedOnNode(SelectTableJoinedOnNode onNode) : this(
        (ExpressionNode)onNode.RightTableNode.Clone(),
        (SelectTableJoinedTypeNode)onNode.JoinTypeNode.Clone(),
        (ExpressionNode)onNode.SearchConditionNode.Clone())
    {
        onNode.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectTableJoinedOnNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var child in base.GetChildren())
        {
            yield return child;
        }
        yield return SearchConditionNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
