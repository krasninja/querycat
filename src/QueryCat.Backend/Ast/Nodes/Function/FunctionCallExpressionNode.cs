namespace QueryCat.Backend.Ast.Nodes.Function;

internal sealed class FunctionCallExpressionNode : ExpressionNode
{
    public FunctionCallNode FunctionNode { get; }

    /// <inheritdoc />
    public override string Code => "expr_fcall";

    /// <inheritdoc />
    public FunctionCallExpressionNode(FunctionCallNode functionNode)
    {
        FunctionNode = functionNode;
    }

    public FunctionCallExpressionNode(FunctionCallExpressionNode node) :
        this((FunctionCallNode)node.FunctionNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return FunctionNode;
    }

    /// <inheritdoc />
    public override object Clone() => new FunctionCallExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => FunctionNode.ToString();
}
