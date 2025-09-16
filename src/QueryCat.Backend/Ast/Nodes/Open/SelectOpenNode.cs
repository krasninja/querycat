using QueryCat.Backend.Ast.Nodes.Select;

namespace QueryCat.Backend.Ast.Nodes.Open;

/// <summary>
/// Special version of <see cref="SelectQueryNode" /> to be able to create select command context.
/// </summary>
internal sealed class SelectOpenNode : SelectQueryNode
{
    /// <inheritdoc />
    public override string Code => "select_open";

    /// <inheritdoc />
    public SelectOpenNode(OpenNode node) : base(
        new SelectColumnsListNode(
            new SelectColumnsSublistAll())
        )
    {
    }

    /// <inheritdoc />
    public SelectOpenNode(SelectQueryNode node) : base(node)
    {
    }

    public SelectOpenNode(SelectOpenNode node) : base(node)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectOpenNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
