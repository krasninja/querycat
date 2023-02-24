namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectTableJoinedUsingNode : SelectTableJoinedNode
{
    public List<string> ColumnList { get; } = new();

    /// <inheritdoc />
    public override string Code => "tablejoinusing";

    public SelectTableJoinedUsingNode(
        ExpressionNode rightTableNode,
        SelectTableJoinedTypeNode joinTypeNode,
        IEnumerable<string> columnList) : base(rightTableNode, joinTypeNode)
    {
        ColumnList.AddRange(columnList);
    }

    public SelectTableJoinedUsingNode(SelectTableJoinedUsingNode node) : this(
        (ExpressionNode)node.RightTableNode.Clone(),
        (SelectTableJoinedTypeNode)node.JoinTypeNode.Clone(),
        node.ColumnList)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectTableJoinedUsingNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
