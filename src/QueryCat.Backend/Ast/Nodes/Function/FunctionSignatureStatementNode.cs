namespace QueryCat.Backend.Ast.Nodes.Function;

public sealed class FunctionSignatureStatementNode : StatementNode
{
    public FunctionSignatureNode FunctionSignatureNode { get; }

    /// <inheritdoc />
    public override string Code => "func_stmt";

    public FunctionSignatureStatementNode(FunctionSignatureNode functionSignatureNode)
    {
        FunctionSignatureNode = functionSignatureNode;
    }

    public FunctionSignatureStatementNode(FunctionSignatureStatementNode node) :
        this((FunctionSignatureNode)node.FunctionSignatureNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override object Clone() => new FunctionSignatureStatementNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return FunctionSignatureNode;
    }
}
