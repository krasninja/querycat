using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Operation that involves three expressions.
/// </summary>
public sealed class TernaryOperationExpressionNode : ExpressionNode
{
    public VariantValue.Operation Operation { get; }

    public ExpressionNode Value { get; }

    public ExpressionNode Left { get; }

    public ExpressionNode Right { get; }

    /// <inheritdoc />
    public override string Code => "ternop";

    /// <inheritdoc />
    public TernaryOperationExpressionNode(VariantValue.Operation operation, ExpressionNode value,
        ExpressionNode left, ExpressionNode right)
    {
        Operation = operation;
        Value = value;
        Left = left;
        Right = right;
    }

    public TernaryOperationExpressionNode(TernaryOperationExpressionNode node)
        : this(node.Operation, (ExpressionNode)node.Value.Clone(),
            (ExpressionNode)node.Left.Clone(), (ExpressionNode)node.Right.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return Value;
        yield return Left;
        yield return Right;
    }

    /// <inheritdoc />
    public override object Clone() => new TernaryOperationExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
