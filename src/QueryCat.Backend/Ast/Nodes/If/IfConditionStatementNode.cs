namespace QueryCat.Backend.Ast.Nodes.If;

internal sealed class IfConditionStatementNode : StatementNode
{
    /// <inheritdoc />
    public IfConditionStatementNode(IfConditionNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public IfConditionStatementNode(StatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override object Clone() => new IfConditionStatementNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
