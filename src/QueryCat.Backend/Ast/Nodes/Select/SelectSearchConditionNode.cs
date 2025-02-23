namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectSearchConditionNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "where_cond";

    public ExpressionNode ExpressionNode { get; }

    public SelectSearchConditionNode(ExpressionNode expressionNode)
    {
        ExpressionNode = expressionNode;
    }

    public SelectSearchConditionNode(SelectSearchConditionNode node) :
        this((ExpressionNode)node.ExpressionNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ExpressionNode;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectSearchConditionNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => $"({ExpressionNode})";
}
