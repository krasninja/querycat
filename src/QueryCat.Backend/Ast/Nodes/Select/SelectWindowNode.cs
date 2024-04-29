namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectWindowNode : AstNode
{
    public List<SelectWindowDefinitionListNode> DefinitionListNodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "window";

    public SelectWindowNode(IEnumerable<SelectWindowDefinitionListNode> nodes)
    {
        DefinitionListNodes.AddRange(nodes);
    }

    public SelectWindowNode(SelectWindowNode node)
        : this(node.DefinitionListNodes.Select(n => (SelectWindowDefinitionListNode)n.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var definitionListNode in DefinitionListNodes)
        {
            yield return definitionListNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectWindowNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
