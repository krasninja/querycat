namespace QueryCat.Backend.Ast.Nodes.For;

internal sealed class ForStatementNode : StatementNode
{
    /// <inheritdoc />
    public ForStatementNode(IAstNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public ForStatementNode(StatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override object Clone() => new ForStatementNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
