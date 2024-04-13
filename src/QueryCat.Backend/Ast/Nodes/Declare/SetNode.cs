namespace QueryCat.Backend.Ast.Nodes.Declare;

internal sealed class SetNode : AstNode
{
    public IdentifierExpressionNode IdentifierNode { get; }

    public StatementNode ValueNode { get; }

    /// <inheritdoc />
    public override string Code => "set_var";

    public SetNode(IdentifierExpressionNode identifierNode, StatementNode valueNode)
    {
        IdentifierNode = identifierNode;
        ValueNode = valueNode;
    }

    public SetNode(SetNode node) : this(
        (IdentifierExpressionNode)node.IdentifierNode.Clone(), (StatementNode)node.ValueNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SetNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return IdentifierNode;
        yield return ValueNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
