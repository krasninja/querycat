namespace QueryCat.Backend.Ast.Nodes.Declare;

public class DeclareStatementNode : StatementNode
{
    /// <inheritdoc />
    public DeclareStatementNode(DeclareNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public DeclareStatementNode(DeclareStatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override object Clone() => new DeclareStatementNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
