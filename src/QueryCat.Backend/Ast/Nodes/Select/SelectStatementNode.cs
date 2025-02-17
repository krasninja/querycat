namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectStatementNode : StatementNode
{
    public SelectQueryNode QueryNode => (SelectQueryNode)RootNode;

    /// <inheritdoc />
    public override string Code => "query_body_stmt";

    public SelectStatementNode(SelectQueryNode queryNode) : base(queryNode)
    {
    }

    public SelectStatementNode(SelectStatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override object Clone() => new SelectStatementNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
