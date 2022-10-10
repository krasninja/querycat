namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// In operation: Expression IN (InExpressionValues).
/// </summary>
public sealed class InOperationExpressionNode : ExpressionNode
{
    public ExpressionNode Expression { get; }

    public InExpressionValuesNode InExpressionValues { get; }

    public bool IsNot { get; }

    /// <inheritdoc />
    public override string Code => "in_expr";

    /// <inheritdoc />
    public InOperationExpressionNode(ExpressionNode expression, ExpressionNode inExpressionValues, bool isNot = false)
    {
        if (inExpressionValues is InExpressionValuesNode inValuesNode)
        {
            InExpressionValues = inValuesNode;
        }
        else
        {
            InExpressionValues = new InExpressionValuesNode(new List<ExpressionNode>()
            {
                inExpressionValues
            });
        }

        Expression = expression;
        IsNot = isNot;
    }

    public InOperationExpressionNode(InOperationExpressionNode node)
        : this(
            (ExpressionNode)node.Expression.Clone(),
            (InExpressionValuesNode)node.InExpressionValues.Clone(),
            isNot: node.IsNot)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return Expression;
        yield return InExpressionValues;
    }

    /// <inheritdoc />
    public override object Clone() => new InOperationExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => $"{Expression} IN {InExpressionValues}";
}
