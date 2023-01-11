using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Operation between two expressions: Left op Right.
/// </summary>
public sealed class BinaryOperationExpressionNode : ExpressionNode
{
    public VariantValue.Operation Operation { get; internal set; }

    public ExpressionNode LeftNode { get; }

    public ExpressionNode RightNode { get; }

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

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return LeftNode;
        yield return RightNode;
    }

    /// <inheritdoc />
    public override object Clone() => new BinaryOperationExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => $"{LeftNode} {Operation} {RightNode}";
}
