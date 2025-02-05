namespace QueryCat.Backend.Ast.Nodes.Call;

internal sealed class CallFunctionStatementNode : StatementNode
{
    /// <inheritdoc />
    public CallFunctionStatementNode(CallFunctionNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public CallFunctionStatementNode(CallFunctionStatementNode node) : base(node)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new CallFunctionStatementNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
