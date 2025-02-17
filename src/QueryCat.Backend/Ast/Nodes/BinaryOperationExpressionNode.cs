using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Operation between two expressions: Left op Right.
/// </summary>
internal sealed class BinaryOperationExpressionNode : ExpressionNode
{
    public VariantValue.Operation Operation { get; internal set; }

    public ExpressionNode LeftNode { get; private set; }

    public ExpressionNode RightNode { get; private set; }

    /// <inheritdoc />
    public override string Code => "binop";

    public BinaryOperationExpressionNode(
        VariantValue.Operation operation, ExpressionNode leftNode, ExpressionNode rightNode)
    {
        Operation = operation;
        LeftNode = leftNode;
        RightNode = rightNode;
    }

    public BinaryOperationExpressionNode(BinaryOperationExpressionNode node) :
        this(node.Operation, (ExpressionNode)node.LeftNode.Clone(), (ExpressionNode)node.RightNode.Clone())
    {
        node.CopyTo(this);
    }

    public bool MatchType<T1, T2>(out T1? value1, out T2? value2)
    {
        if (LeftNode is T1 left1 && RightNode is T2 right1)
        {
            value1 = left1;
            value2 = right1;
            return true;
        }
        if (LeftNode is T2 left2 && RightNode is T1 right2)
        {
            value1 = right2;
            value2 = left2;
            return true;
        }
        value1 = default;
        value2 = default;
        return false;
    }

    /// <summary>
    /// Tries to swap left and right expressions.
    /// </summary>
    /// <returns><c>True</c> if can swap, <c>false</c> otherwise.</returns>
    public bool TryReverse()
    {
        var reverseOperation = GetReverseOperation(Operation);
        if (reverseOperation.HasValue)
        {
            (LeftNode, RightNode) = (RightNode, LeftNode);
            Operation = reverseOperation.Value;
            return true;
        }

        return false;
    }

    private static VariantValue.Operation? GetReverseOperation(VariantValue.Operation operation)
        => operation switch
        {
            VariantValue.Operation.Equals => VariantValue.Operation.Equals,
            VariantValue.Operation.Greater => VariantValue.Operation.Less,
            VariantValue.Operation.GreaterOrEquals => VariantValue.Operation.LessOrEquals,
            VariantValue.Operation.Less => VariantValue.Operation.Greater,
            VariantValue.Operation.LessOrEquals => VariantValue.Operation.GreaterOrEquals,
            _ => null,
        };

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return LeftNode;
        yield return RightNode;
    }

    /// <inheritdoc />
    public override object Clone() => new BinaryOperationExpressionNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => $"{LeftNode} {Operation} {RightNode}";
}
