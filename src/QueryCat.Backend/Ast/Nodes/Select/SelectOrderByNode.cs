namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectOrderByNode : AstNode
{
    public List<SelectOrderBySpecificationNode> OrderBySpecificationNodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "orderby";

    public SelectOrderByNode(IEnumerable<SelectOrderBySpecificationNode> orderBySpecificationNodes)
    {
        OrderBySpecificationNodes.AddRange(orderBySpecificationNodes);
    }

    public SelectOrderByNode(SelectOrderByNode node)
        : this(node.OrderBySpecificationNodes.Select(n => (SelectOrderBySpecificationNode)n.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => OrderBySpecificationNodes;

    /// <inheritdoc />
    public override object Clone() => new SelectOrderByNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
