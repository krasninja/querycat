namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectOrderByNode : AstNode
{
    public IList<SelectOrderBySpecificationNode> OrderBySpecificationNodes { get; }

    /// <inheritdoc />
    public override string Code => "orderby";

    public SelectOrderByNode(IList<SelectOrderBySpecificationNode> orderBySpecificationNodes)
    {
        OrderBySpecificationNodes = orderBySpecificationNodes;
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
}
