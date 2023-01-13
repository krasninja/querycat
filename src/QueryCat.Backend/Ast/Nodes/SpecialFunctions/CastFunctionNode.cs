using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast.Nodes.SpecialFunctions;

public sealed class CastFunctionNode : ExpressionNode
{
    public ExpressionNode ExpressionNode { get; }

    public TypeNode TargetTypeNode { get; }

    /// <inheritdoc />
    public override string Code => "cast";

    /// <inheritdoc />
    public CastFunctionNode(ExpressionNode expressionNode, TypeNode targetTypeNode)
    {
        if (targetTypeNode.Type == DataType.Void)
        {
            throw new SemanticException("Cannot cast to type VOID.");
        }
        ExpressionNode = expressionNode;
        TargetTypeNode = targetTypeNode;
    }

    public CastFunctionNode(CastFunctionNode node) : this(
        (ExpressionNode)node.ExpressionNode.Clone(), (TypeNode)node.TargetTypeNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ExpressionNode;
    }

    /// <inheritdoc />
    public override object Clone() => new CastFunctionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => $"Cast {ExpressionNode} As {TargetTypeNode}";
}
