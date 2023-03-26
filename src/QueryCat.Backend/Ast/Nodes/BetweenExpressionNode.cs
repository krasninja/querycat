using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Between expression: Expression BETWEEN Left AND Right.
/// </summary>
public sealed class BetweenExpressionNode : ExpressionNode
{
    public ExpressionNode Expression { get; }

    public ExpressionNode AndExpression { get; }

    public bool IsNot { get; }

    public ExpressionNode Left { get; }

    public ExpressionNode Right { get; }

    /// <inheritdoc />
    public override string Code => "betweenop";

    /// <inheritdoc />
    public BetweenExpressionNode(ExpressionNode expression, ExpressionNode andExpression, bool isNot = false)
    {
        Expression = expression;
        AndExpression = andExpression;
        IsNot = isNot;
        if (AndExpression is BinaryOperationExpressionNode binaryOperationExpressionNode
            && (binaryOperationExpressionNode.Operation == VariantValue.Operation.And
                || binaryOperationExpressionNode.Operation == VariantValue.Operation.BetweenAnd))
        {
            Left = binaryOperationExpressionNode.LeftNode;
            Right = binaryOperationExpressionNode.RightNode;
            // Replace AND comparision by any another operation just to bypass types resolve.
            // The correct delegate will be generated anyway.
            binaryOperationExpressionNode.Operation = VariantValue.Operation.BetweenAnd;
        }
        else
        {
            throw new SemanticException("Invalid BETWEEN expression.");
        }
    }

    public BetweenExpressionNode(BetweenExpressionNode node)
        : this((ExpressionNode)node.Expression.Clone(), (ExpressionNode)node.AndExpression.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return Expression;
        yield return AndExpression;
    }

    /// <inheritdoc />
    public override object Clone() => new BetweenExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
