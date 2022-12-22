namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectStatementNode : StatementNode
{
    public SelectQueryNode QueryNode { get; }

    /// <inheritdoc />
    public override string Code => "query_body_stmt";

    public SelectStatementNode(SelectQueryNode queryNode)
    {
        QueryNode = queryNode;
    }

    public SelectStatementNode(SelectStatementNode node) :
        this((SelectQueryNode)node.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return QueryNode;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectStatementNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => $"{QueryNode}";
}
