using QueryCat.Backend.Core;

namespace QueryCat.Backend.Ast.Nodes.Break;

internal sealed class BreakStatementNode : StatementNode, ICommandNode
{
    /// <inheritdoc />
    public string CommandName => Application.CommandBreak;

    /// <inheritdoc />
    public BreakStatementNode(IAstNode rootNode) : base(rootNode)
    {
    }

    /// <inheritdoc />
    public BreakStatementNode(BreakStatementNode node) : base(node)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new BreakStatementNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
