using System.Text;

namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectWithNode : AstNode
{
    public List<SelectColumnsSublistNameNode> ColumnNodes { get; } = new();

    public SelectQueryExpressionBodyNode QueryNode { get; }

    public string Name { get; }

    /// <inheritdoc />
    public override string Code => "with_elem";

    /// <inheritdoc />
    public SelectWithNode(string name, SelectQueryExpressionBodyNode queryNode)
    {
        Name = name;
        QueryNode = queryNode;
    }

    public SelectWithNode(SelectWithNode node)
        : this(node.Name, (SelectQueryExpressionBodyNode)node.QueryNode.Clone())
    {
        ColumnNodes = node.ColumnNodes.Select(c => (SelectColumnsSublistNameNode)c.Clone()).ToList();
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectWithNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var column in ColumnNodes)
        {
            yield return column;
        }
        yield return QueryNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        if (ColumnNodes.Count > 0)
        {
            sb.Append($" ({string.Join(", ", ColumnNodes.Select(c => c.ToString()))})");
        }
        sb.Append($" {Name} AS ({QueryNode})");
        return sb.ToString();
    }
}
