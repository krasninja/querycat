namespace QueryCat.Backend.Ast.Nodes.Return;

internal sealed class ReturnNode : AstNode
{
    public ExpressionNode? ReturnExpression { get; }

    /// <inheritdoc />
    public override string Code => "return";

    public ReturnNode(ExpressionNode? returnExpression)
    {
        ReturnExpression = returnExpression;
    }

    public ReturnNode(ReturnNode node)
    {
        if (node.ReturnExpression != null)
        {
            ReturnExpression = (ExpressionNode)node.ReturnExpression.Clone();
        }
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        if (ReturnExpression != null)
        {
            yield return ReturnExpression;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new ReturnNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
