namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectQueryExpressionBodyNode : ExpressionNode
{
    public SelectQuerySpecificationNode[] Queries { get; }

    public string Alias { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public override string Code => "select_query_body";

    public SelectOrderByNode? OrderBy { get; set; }

    public SelectOffsetNode? Offset { get; set; }

    public SelectFetchNode? Fetch { get; set; }

    /// <inheritdoc />
    public SelectQueryExpressionBodyNode(params SelectQuerySpecificationNode[] queries)
    {
        Queries = queries;
    }

    public SelectQueryExpressionBodyNode(SelectQueryExpressionBodyNode node) :
        this(node.Queries.Select(q => (SelectQuerySpecificationNode)q.Clone()).ToArray())
    {
        if (node.Offset != null)
        {
            Offset = (SelectOffsetNode)node.Offset.Clone();
        }
        if (node.Fetch != null)
        {
            Fetch = (SelectFetchNode)node.Fetch.Clone();
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
        if (OrderBy != null)
        {
            yield return OrderBy;
        }
        if (Offset != null)
        {
            yield return Offset;
        }
        if (Fetch != null)
        {
            yield return Fetch;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectQueryExpressionBodyNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
