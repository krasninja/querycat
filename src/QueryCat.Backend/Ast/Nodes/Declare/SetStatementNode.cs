using QueryCat.Backend.Core;

namespace QueryCat.Backend.Ast.Nodes.Declare;

internal sealed class SetStatementNode : StatementNode, ICommandNode
{
    /// <inheritdoc />
    public string CommandName => Application.CommandSet;

    /// <inheritdoc />
    public SetStatementNode(SetNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public SetStatementNode(SetStatementNode node) : base(node)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SetStatementNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
