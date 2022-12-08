using System.Text;

namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectWithNode : AstNode
{
    public List<SelectColumnsSublistNameNode> Columns { get; } = new();

    public SelectQuerySpecificationNode Query { get; }

    public string Name { get; }

    /// <inheritdoc />
    public override string Code => "with_elem";

    /// <inheritdoc />
    public SelectWithNode(string name, SelectQuerySpecificationNode query)
    {
        Name = name;
        Query = query;
    }

    public SelectWithNode(SelectWithNode node)
        : this(node.Name, (SelectQuerySpecificationNode)node.Query.Clone())
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

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        if (Columns.Count > 0)
        {
            sb.Append($" ({string.Join(", ", Columns.Select(c => c.ToString()))})");
        }
        sb.Append($" {Name} AS ({Query})");
        return sb.ToString();
    }
}
