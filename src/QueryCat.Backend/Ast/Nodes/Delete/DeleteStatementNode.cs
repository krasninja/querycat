namespace QueryCat.Backend.Ast.Nodes.Delete;

internal sealed class DeleteStatementNode : StatementNode
{
    /// <inheritdoc />
    public DeleteStatementNode(IAstNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public DeleteStatementNode(StatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override object Clone() => new DeleteStatementNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
