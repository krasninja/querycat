using QueryCat.Backend.Core;

namespace QueryCat.Backend.Ast.Nodes.Open;

internal sealed class OpenStatementNode : StatementNode, ICommandNode
{
    /// <inheritdoc />
    public string CommandName => Application.CommandOpen;

    /// <inheritdoc />
    public OpenStatementNode(IAstNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public OpenStatementNode(OpenStatementNode node) : base(node)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new OpenStatementNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
