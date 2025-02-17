using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Operation that involves three expressions.
/// </summary>
internal sealed class TernaryOperationExpressionNode : ExpressionNode
{
    public VariantValue.Operation Operation { get; }

    public ExpressionNode ValueNode { get; }

    public ExpressionNode LeftNode { get; }

    public ExpressionNode RightNode { get; }

    /// <inheritdoc />
    public override string Code => "ternop";

    /// <inheritdoc />
    public TernaryOperationExpressionNode(VariantValue.Operation operation, ExpressionNode valueNode,
        ExpressionNode leftNode, ExpressionNode rightNode)
    {
        Operation = operation;
        ValueNode = valueNode;
        LeftNode = leftNode;
        RightNode = rightNode;
    }

    public TernaryOperationExpressionNode(TernaryOperationExpressionNode node)
        : this(node.Operation, (ExpressionNode)node.ValueNode.Clone(),
            (ExpressionNode)node.LeftNode.Clone(), (ExpressionNode)node.RightNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ValueNode;
        yield return LeftNode;
        yield return RightNode;
    }

    /// <inheritdoc />
    public override object Clone() => new TernaryOperationExpressionNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
