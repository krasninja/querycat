using QueryCat.Backend.Core;

namespace QueryCat.Backend.Ast.Nodes.Update;

internal sealed class UpdateStatementNode : StatementNode, ICommandNode
{
    /// <inheritdoc />
    public string CommandName => Application.CommandUpdate;

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
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
