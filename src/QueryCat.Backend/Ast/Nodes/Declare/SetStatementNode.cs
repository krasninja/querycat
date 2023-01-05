namespace QueryCat.Backend.Ast.Nodes.Declare;

public class SetStatementNode : StatementNode
{
    /// <inheritdoc />
    public SetStatementNode(SetNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public SetStatementNode(SetStatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override object Clone() => new SetStatementNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
