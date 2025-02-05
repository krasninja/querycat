namespace QueryCat.Backend.Ast.Nodes.Select;

/// <summary>
/// Select alias (SELECT name as alias_name).
/// </summary>
internal sealed class SelectAliasNode : AstNode
{
    public static SelectAliasNode Empty { get; } = new(string.Empty);

    public string AliasName { get; }

    public SelectAliasNode(string aliasName)
    {
        AliasName = aliasName;
    }

    public SelectAliasNode(SelectAliasNode node) : this(node.AliasName)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override string Code => "alias";

    /// <inheritdoc />
    public override object Clone() => new SelectAliasNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => AliasName;
}
