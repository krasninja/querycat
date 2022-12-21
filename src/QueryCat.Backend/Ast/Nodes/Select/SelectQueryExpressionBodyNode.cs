using System.Text;

namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectQueryExpressionBodyNode : ExpressionNode
{
    public SelectQuerySpecificationNode QueryNode { get; }

    public string Alias { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public override string Code => "select_query_body";

    public SelectOrderByNode? OrderByNode { get; set; }

    public SelectOffsetNode? OffsetNode { get; set; }

    public SelectFetchNode? FetchNode { get; set; }

    /// <inheritdoc />
    public SelectQueryExpressionBodyNode(SelectQuerySpecificationNode queryNode)
    {
        QueryNode = queryNode;
    }

    public SelectQueryExpressionBodyNode(SelectQueryExpressionBodyNode node) :
        this((SelectQuerySpecificationNode)node.QueryNode.Clone())
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
        yield return QueryNode;
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
        var sb = new StringBuilder()
            .Append('(')
            .Append(QueryNode);
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
        sb.Append(')');
        return sb.ToString();
    }
}
