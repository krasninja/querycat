using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Operation between two expressions: Left op Right.
/// </summary>
public sealed class BinaryOperationExpressionNode : ExpressionNode
{
    public VariantValue.Operation Operation { get; internal set; }

    public ExpressionNode Left { get; }

    public ExpressionNode Right { get; }

    /// <inheritdoc />
    public override string Code => "binop";

    public BinaryOperationExpressionNode(
        VariantValue.Operation operation, ExpressionNode left, ExpressionNode right)
    {
        Operation = operation;
        Left = left;
        Right = right;
    }

    public BinaryOperationExpressionNode(BinaryOperationExpressionNode node) :
        this(node.Operation, (ExpressionNode)node.Left.Clone(), (ExpressionNode)node.Right.Clone())
    {
        node.CopyTo(this);
    }

    public bool MatchType<T1, T2>(out T1? value1, out T2? value2)
    {
        if (Left is T1 left1 && Right is T2 right1)
        {
            value1 = left1;
            value2 = right1;
            return true;
        }
        if (Left is T2 left2 && Right is T1 right2)
        {
            value1 = right2;
            value2 = left2;
            return true;
        }
        value1 = default;
        value2 = default;
        return false;
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return Left;
        yield return Right;
    }

    /// <inheritdoc />
    public override object Clone() => new BinaryOperationExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => $"{Left} {Operation} {Right}";
}
