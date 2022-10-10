namespace QueryCat.Backend.Ast.Nodes;

public sealed class ExpressionStatementNode : StatementNode
{
    public ExpressionNode ExpressionNode { get; }

    /// <inheritdoc />
    public override string Code => "expr_stmt";

    public ExpressionStatementNode(ExpressionNode expressionNode)
    {
        ExpressionNode = expressionNode;
    }

    public ExpressionStatementNode(ExpressionStatementNode node) :
        this((ExpressionNode)node.ExpressionNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ExpressionNode;
    }

    /// <inheritdoc />
    public override object Clone() => new ExpressionStatementNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
