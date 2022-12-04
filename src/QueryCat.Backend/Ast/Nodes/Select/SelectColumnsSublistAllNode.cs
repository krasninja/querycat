namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectColumnsSublistAll : SelectColumnsSublistNode
{
    /// <inheritdoc />
    public override string Code => "column_all";

    public SelectColumnsSublistAll()
    {
    }

    public SelectColumnsSublistAll(SelectColumnsSublistAll node)
    {
        Alias = node.Alias;
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsSublistAll(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => "*";
}
