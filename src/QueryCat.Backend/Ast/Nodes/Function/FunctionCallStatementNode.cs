namespace QueryCat.Backend.Ast.Nodes.Function;

public sealed class FunctionCallStatementNode : StatementNode
{
    public FunctionCallNode FunctionNode { get; }

    /// <inheritdoc />
    public override string Code => "fcall_stmt";

    /// <inheritdoc />
    public FunctionCallStatementNode(FunctionCallNode functionNode)
    {
        FunctionNode = functionNode;
    }

    public FunctionCallStatementNode(FunctionCallStatementNode node) :
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
    public override object Clone() => new FunctionCallStatementNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
