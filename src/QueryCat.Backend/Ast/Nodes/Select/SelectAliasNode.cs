namespace QueryCat.Backend.Ast.Nodes.Select;

/// <summary>
/// Select alias (SELECT name as alias_name).
/// </summary>
public sealed class SelectAliasNode : AstNode
{
    public string AliasName { get; }

    public static SelectAliasNode Empty { get; } = new(string.Empty);

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
    public override string ToString() => AliasName;
}
