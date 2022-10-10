namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectTableExpressionNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "table_expr";

    public SelectTableReferenceListNode Tables { get; }

    public SelectSearchConditionNode? SearchConditionNode { get; set; }

    public SelectGroupByNode? GroupByNode { get; set; }

    public SelectHavingNode? HavingNode { get; set; }

    public SelectTableExpressionNode(SelectTableReferenceListNode selectTableReferenceListNode)
    {
        Tables = selectTableReferenceListNode;
    }

    public SelectTableExpressionNode(SelectTableExpressionNode node)
    {
        Tables = (SelectTableReferenceListNode)node.Tables.Clone();
        if (node.SearchConditionNode != null)
        {
            SearchConditionNode = (SelectSearchConditionNode)node.SearchConditionNode.Clone();
        }
        if (node.GroupByNode != null)
        {
            GroupByNode = (SelectGroupByNode)node.GroupByNode.Clone();
        }
        if (node.HavingNode != null)
        {
            HavingNode = (SelectHavingNode)node.HavingNode.Clone();
        }
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return Tables;
        if (SearchConditionNode != null)
        {
            yield return SearchConditionNode;
        }
        if (GroupByNode != null)
        {
            yield return GroupByNode;
        }
        if (HavingNode != null)
        {
            yield return HavingNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectTableExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
