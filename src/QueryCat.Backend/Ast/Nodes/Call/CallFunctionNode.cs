using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Call;

internal sealed class CallFunctionNode : AstNode
{
    public FunctionCallNode FunctionCallNode { get; }

    /// <inheritdoc />
    public override string Code => "call";

    /// <inheritdoc />
    public CallFunctionNode(FunctionCallNode functionCallNode)
    {
        FunctionCallNode = functionCallNode;
    }

    public CallFunctionNode(CallFunctionNode node) : this((FunctionCallNode)node.FunctionCallNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new CallFunctionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
