using QueryCat.Backend.Core;

namespace QueryCat.Backend.Ast.Nodes.Declare;

internal class DeclareStatementNode : StatementNode, ICommandNode
{
    /// <inheritdoc />
    public string CommandName => Application.CommandDeclare;

    /// <inheritdoc />
    public DeclareStatementNode(DeclareNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public DeclareStatementNode(DeclareStatementNode node) : base(node)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new DeclareStatementNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
