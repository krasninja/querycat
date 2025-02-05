namespace QueryCat.Backend.Ast.Nodes.Function;

internal sealed class FunctionSignatureStatementNode : StatementNode
{
    public FunctionSignatureNode FunctionSignatureNode => (FunctionSignatureNode)RootNode;

    public FunctionSignatureStatementNode(FunctionSignatureNode functionSignatureNode) : base(functionSignatureNode)
    {
    }

    public FunctionSignatureStatementNode(FunctionSignatureStatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override object Clone() => new FunctionSignatureStatementNode(this);
}
