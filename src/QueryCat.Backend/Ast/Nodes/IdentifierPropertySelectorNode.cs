namespace QueryCat.Backend.Ast.Nodes;

internal sealed class IdentifierPropertySelectorNode : IdentifierSelectorNode
{
    /// <inheritdoc />
    public override string Code => "id_prop_selector";

    public string PropertyName { get; }

    /// <inheritdoc />
    public IdentifierPropertySelectorNode(string propertyName)
    {
        PropertyName = propertyName;
    }

    public IdentifierPropertySelectorNode(IdentifierPropertySelectorNode node) : this(node.PropertyName)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new IdentifierPropertySelectorNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => "." + PropertyName;
}
