namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectWindowOrderClauseNode : AstNode
{
    public List<SelectOrderBySpecificationNode> OrderBySpecificationNodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "window_order";

    public SelectWindowOrderClauseNode(IEnumerable<SelectOrderBySpecificationNode> nodes)
    {
        OrderBySpecificationNodes.AddRange(nodes);
    }

    public SelectWindowOrderClauseNode(SelectWindowOrderClauseNode node)
        : this(node.OrderBySpecificationNodes.Select(n => (SelectOrderBySpecificationNode)n.Clone()))
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var orderBySpecificationNode in OrderBySpecificationNodes)
        {
            yield return orderBySpecificationNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectWindowOrderClauseNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
