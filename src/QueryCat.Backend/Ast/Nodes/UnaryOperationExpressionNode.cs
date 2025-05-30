using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Unary operation.
/// </summary>
internal sealed class UnaryOperationExpressionNode : ExpressionNode
{
    public VariantValue.Operation Operation { get; }

    public ExpressionNode RightNode { get; }

    /// <inheritdoc />
    public override string Code => "unaryop";

    /// <inheritdoc />
    public UnaryOperationExpressionNode(VariantValue.Operation operation, ExpressionNode rightNode)
    {
        Operation = operation;
        RightNode = rightNode;
    }

    public UnaryOperationExpressionNode(UnaryOperationExpressionNode node) :
        this(node.Operation, (ExpressionNode)node.RightNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return RightNode;
    }

    /// <inheritdoc />
    public override object Clone() => new UnaryOperationExpressionNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => $"{Operation} {RightNode}";
}
