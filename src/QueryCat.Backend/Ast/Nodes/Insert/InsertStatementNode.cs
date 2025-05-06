using QueryCat.Backend.Core;

namespace QueryCat.Backend.Ast.Nodes.Insert;

internal sealed class InsertStatementNode : StatementNode, ICommandNode
{
    /// <inheritdoc />
    public string CommandName => Application.CommandInsert;

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
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
