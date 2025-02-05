namespace QueryCat.Backend.Ast.Nodes;

internal sealed class ExpressionStatementNode : StatementNode
{
    public ExpressionNode ExpressionNode => (ExpressionNode)RootNode;

    /// <inheritdoc />
    public override string Code => "expr_stmt";

    public ExpressionStatementNode(ExpressionNode expressionNode) : base(expressionNode)
    {
    }

    public ExpressionStatementNode(ExpressionStatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override object Clone() => new ExpressionStatementNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
