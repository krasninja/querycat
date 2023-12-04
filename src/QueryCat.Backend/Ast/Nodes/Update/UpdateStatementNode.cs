namespace QueryCat.Backend.Ast.Nodes.Update;

internal sealed class UpdateStatementNode : StatementNode
{
    /// <inheritdoc />
    public UpdateStatementNode(UpdateNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public UpdateStatementNode(UpdateStatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override object Clone() => new UpdateStatementNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
