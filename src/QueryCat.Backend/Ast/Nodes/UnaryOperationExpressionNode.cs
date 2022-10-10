using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Unary operation.
/// </summary>
public sealed class UnaryOperationExpressionNode : ExpressionNode
{
    public VariantValue.Operation Operation { get; }

    public ExpressionNode Right { get; }

    /// <inheritdoc />
    public override string Code => "unaryop";

    /// <inheritdoc />
    public UnaryOperationExpressionNode(VariantValue.Operation operation, ExpressionNode right)
    {
        Operation = operation;
        Right = right;
    }

    public UnaryOperationExpressionNode(UnaryOperationExpressionNode node) :
        this(node.Operation, (ExpressionNode)node.Right.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return Right;
    }

    /// <inheritdoc />
    public override object Clone() => new UnaryOperationExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => $"{Operation} {Right}";
}
