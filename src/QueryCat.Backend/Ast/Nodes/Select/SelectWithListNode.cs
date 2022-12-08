namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectWithListNode : AstNode
{
    public List<SelectWithNode> Nodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "with";

    public SelectWithListNode(List<SelectWithNode> withNodes)
    {
        Nodes.AddRange(withNodes);
    }

    public SelectWithListNode(SelectWithListNode node)
        : this(node.Nodes.Select(c => (SelectWithNode)c.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectWithListNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var withNode in Nodes)
        {
            yield return withNode;
        }
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => string.Join(", ", Nodes.Select(n => n.ToString()));
}
