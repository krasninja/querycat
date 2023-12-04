namespace QueryCat.Backend.Ast.Nodes.Insert;

internal sealed class InsertStatementNode : StatementNode
{
    /// <inheritdoc />
    public InsertStatementNode(InsertNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public InsertStatementNode(InsertStatementNode node) : base(node)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new InsertStatementNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
