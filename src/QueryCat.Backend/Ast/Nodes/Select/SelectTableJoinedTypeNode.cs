namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectTableJoinedTypeNode : AstNode
{
    public SelectTableJoinedType JoinedType { get; }

    /// <inheritdoc />
    public override string Code => "tablejoin_type";

    /// <inheritdoc />
    public SelectTableJoinedTypeNode(SelectTableJoinedType joinedType)
    {
        JoinedType = joinedType;
    }

    public SelectTableJoinedTypeNode(SelectTableJoinedTypeNode node)
        : this(node.JoinedType)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectTableJoinedTypeNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
