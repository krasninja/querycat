namespace QueryCat.Backend.Ast.Nodes.Select;

internal abstract class SelectColumnsSublistNode : AstNode
{
    public string Alias { get; internal set; } = string.Empty;

    public SelectColumnsSublistNode()
    {
    }

    public SelectColumnsSublistNode(SelectColumnsSublistNode node)
    {
        Alias = node.Alias;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
