namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectQueryExpressionBodyNode : ExpressionNode
{
    public SelectQuerySpecificationNode[] Queries { get; }

    public string Alias { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public override string Code => "select_query_body";

    /// <inheritdoc />
    public SelectQueryExpressionBodyNode(params SelectQuerySpecificationNode[] queries)
    {
        Queries = queries;
    }

    public SelectQueryExpressionBodyNode(SelectQueryExpressionBodyNode node) :
        this(node.Queries.Select(q => (SelectQuerySpecificationNode)q.Clone()).ToArray())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => Queries;

    /// <inheritdoc />
    public override object Clone() => new SelectQueryExpressionBodyNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
