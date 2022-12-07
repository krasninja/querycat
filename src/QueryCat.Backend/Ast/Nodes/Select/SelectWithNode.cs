namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectWithNode : AstNode
{
    public List<SelectColumnsSublistNameNode> Columns { get; } = new();

    public SelectQuerySpecificationNode Query { get; }

    /// <inheritdoc />
    public override string Code => "with_elem";

    /// <inheritdoc />
    public SelectWithNode(SelectQuerySpecificationNode query)
    {
        Query = query;
    }

    public SelectWithNode(SelectWithNode node)
        : this((SelectQuerySpecificationNode)node.Query.Clone())
    {
        Columns = node.Columns.Select(c => (SelectColumnsSublistNameNode)c.Clone()).ToList();
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectWithNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var column in Columns)
        {
            yield return column;
        }
        yield return Query;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
