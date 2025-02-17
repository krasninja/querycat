namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// In operation: Expression IN (InExpressionValues).
/// </summary>
internal sealed class InOperationExpressionNode : ExpressionNode
{
    public ExpressionNode ExpressionNode { get; }

    public ExpressionNode InExpressionValuesNodes { get; }

    public bool IsNot { get; }

    /// <inheritdoc />
    public override string Code => "in_expr";

    /// <inheritdoc />
    public InOperationExpressionNode(ExpressionNode expressionNode, ExpressionNode inExpressionValues, bool isNot = false)
    {
        ExpressionNode = expressionNode;
        InExpressionValuesNodes = inExpressionValues;
        IsNot = isNot;
    }

    public InOperationExpressionNode(InOperationExpressionNode node)
        : this(
            (ExpressionNode)node.ExpressionNode.Clone(),
            (ExpressionNode)node.InExpressionValuesNodes.Clone(),
            isNot: node.IsNot)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ExpressionNode;
        yield return InExpressionValuesNodes;
    }

    /// <inheritdoc />
    public override object Clone() => new InOperationExpressionNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => $"{ExpressionNode} IN {InExpressionValuesNodes}";
}
