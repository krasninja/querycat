using QueryCat.Backend.Core;

namespace QueryCat.Backend.Ast.Nodes.Delete;

internal sealed class DeleteStatementNode : StatementNode, ICommandNode
{
    /// <inheritdoc />
    public string CommandName => Application.CommandDelete;

    /// <inheritdoc />
    public DeleteStatementNode(IAstNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public DeleteStatementNode(StatementNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override object Clone() => new DeleteStatementNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
