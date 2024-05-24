using System.Text;

namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectTableNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "table";

    public SelectTableReferenceListNode TablesNode { get; }

    public SelectSearchConditionNode? SearchConditionNode { get; set; }

    public SelectGroupByNode? GroupByNode { get; set; }

    public SelectHavingNode? HavingNode { get; set; }

    public SelectTableNode(SelectTableReferenceListNode selectTableNodeReferenceListNode)
    {
        TablesNode = selectTableNodeReferenceListNode;
    }

    public SelectTableNode(params ExpressionNode[] expressionNodes)
    {
        TablesNode = new SelectTableReferenceListNode(expressionNodes);
    }

    public SelectTableNode(SelectTableNode node)
    {
        TablesNode = (SelectTableReferenceListNode)node.TablesNode.Clone();
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
        yield return TablesNode;
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
    public override object Clone() => new SelectTableNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(", ", TablesNode.ToString()));
        if (SearchConditionNode != null)
        {
            sb.Append($" Where {SearchConditionNode}");
        }
        if (GroupByNode != null)
        {
            sb.Append($" Group By {GroupByNode}");
        }
        if (HavingNode != null)
        {
            sb.Append($" Having {HavingNode}");
        }
        return sb.ToString();
    }
}
