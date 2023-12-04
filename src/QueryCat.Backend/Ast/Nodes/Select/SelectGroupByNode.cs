namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectGroupByNode : AstNode
{
    public List<ExpressionNode> GroupByNodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "groupby";

    public SelectGroupByNode(IEnumerable<ExpressionNode> groupByNodes)
    {
        GroupByNodes.AddRange(groupByNodes);
    }

    public SelectGroupByNode(SelectGroupByNode node) :
        this(node.GroupByNodes.Select(g => (ExpressionNode)g.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => GroupByNodes;

    /// <inheritdoc />
    public override object Clone() => new SelectGroupByNode(this);
}
