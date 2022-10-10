namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectStatementNode : StatementNode
{
    public SelectQueryExpressionBodyNode QueryNode { get; }

    /// <inheritdoc />
    public override string Code => "query_body_stmt";

    public SelectStatementNode(SelectQueryExpressionBodyNode queryNode)
    {
        QueryNode = queryNode;
    }

    public SelectStatementNode(SelectStatementNode node) :
        this((SelectQueryExpressionBodyNode)node.QueryNode.Clone())
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
}
