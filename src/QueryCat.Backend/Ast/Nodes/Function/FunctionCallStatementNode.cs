namespace QueryCat.Backend.Ast.Nodes.Function;

internal sealed class FunctionCallStatementNode : StatementNode
{
    public FunctionCallNode FunctionNode => (FunctionCallNode)RootNode;

    /// <inheritdoc />
    public FunctionCallStatementNode(FunctionCallNode functionNode) : base(functionNode)
    {
    }

    public FunctionCallStatementNode(FunctionCallStatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override object Clone() => new FunctionCallStatementNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
