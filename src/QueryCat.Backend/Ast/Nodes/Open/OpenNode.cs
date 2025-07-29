namespace QueryCat.Backend.Ast.Nodes.Open;

internal sealed class OpenNode : AstNode
{
    public ExpressionNode Expression { get; }

    /// <inheritdoc />
    public override string Code => "open";

    /// <inheritdoc />
    public OpenNode(ExpressionNode expression)
    {
        Expression = expression;
    }

    /// <summary>
    /// CLone constructor.
    /// </summary>
    /// <param name="node">Instance of <see cref="OpenNode" /> to clone from.</param>
    public OpenNode(OpenNode node)
    {
        Expression = (ExpressionNode)node.Expression.Clone();
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new OpenNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
