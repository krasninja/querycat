namespace QueryCat.Backend.Ast.Nodes.Function;

public sealed class FunctionCallStatementNode : StatementNode
{
    public FunctionCallNode FunctionNode => (FunctionCallNode)RootNode;

    /// <inheritdoc />
    public FunctionCallStatementNode(FunctionCallNode functionNode) : base(functionNode)
    {
    }

    public FunctionCallStatementNode(FunctionCallStatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override object Clone() => new FunctionCallStatementNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
