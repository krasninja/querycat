using System.Text;

namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectQueryExpressionBodyNode : ExpressionNode
{
    public SelectQuerySpecificationNode[] Queries { get; }

    public string Alias { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public override string Code => "select_query_body";

    public SelectOrderByNode? OrderByNode { get; set; }

    public SelectOffsetNode? OffsetNode { get; set; }

    public SelectFetchNode? FetchNode { get; set; }

    /// <inheritdoc />
    public SelectQueryExpressionBodyNode(params SelectQuerySpecificationNode[] queries)
    {
        Queries = queries;
    }

    public SelectQueryExpressionBodyNode(SelectQueryExpressionBodyNode node) :
        this(node.Queries.Select(q => (SelectQuerySpecificationNode)q.Clone()).ToArray())
    {
        if (node.OffsetNode != null)
        {
            OffsetNode = (SelectOffsetNode)node.OffsetNode.Clone();
        }
        if (node.FetchNode != null)
        {
            FetchNode = (SelectFetchNode)node.FetchNode.Clone();
        }
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var query in Queries)
        {
            yield return query;
        }
        if (OrderByNode != null)
        {
            yield return OrderByNode;
        }
        if (OffsetNode != null)
        {
            yield return OffsetNode;
        }
        if (FetchNode != null)
        {
            yield return FetchNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectQueryExpressionBodyNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append('(');
        sb.Append(string.Join(" Union ", Queries.Select(q => q.ToString())));
        sb.Append(')');
        if (OffsetNode != null)
        {
            sb.Append($" Offset {OffsetNode}");
        }
        if (FetchNode != null)
        {
            sb.Append($" Fetch {FetchNode}");
        }
        if (OrderByNode != null)
        {
            sb.Append($" Order {OrderByNode}");
        }
        return sb.ToString();
    }
}
