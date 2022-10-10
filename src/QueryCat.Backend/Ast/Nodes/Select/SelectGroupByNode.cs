namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectGroupByNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "groupby";

    public IList<ExpressionNode> GroupBy { get; }

    public SelectGroupByNode(IList<ExpressionNode> groupBy)
    {
        GroupBy = groupBy;
    }

    public SelectGroupByNode(SelectGroupByNode node) :
        this(node.GroupBy.Select(g => (ExpressionNode)g.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => GroupBy;

    /// <inheritdoc />
    public override object Clone() => new SelectGroupByNode(this);
}
