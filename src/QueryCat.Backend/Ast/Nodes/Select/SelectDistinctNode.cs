namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectDistinctNode : AstNode
{
    /// <summary>
    /// Make distinct by all values.
    /// </summary>
    public bool IsAll { get; private set; }

    /// <summary>
    /// Ignore distinct at all.
    /// </summary>
    public bool IsEmpty { get; private set; }

    /// <inheritdoc />
    public override string Code => "distinct";

    public List<ExpressionNode> OnNodes { get; } = new();

    public static SelectDistinctNode Empty { get; } = new()
    {
        IsEmpty = true
    };

    public static SelectDistinctNode All { get; } = new()
    {
        IsAll = true
    };

    private SelectDistinctNode()
    {
    }

    public SelectDistinctNode(IEnumerable<ExpressionNode> onNodes)
    {
        OnNodes.AddRange(onNodes);
    }

    public SelectDistinctNode(SelectDistinctNode node) :
        this(node.OnNodes.Select(g => (ExpressionNode)g.Clone()).ToList())
    {
        IsAll = node.IsAll;
        IsEmpty = node.IsEmpty;
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectDistinctNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => OnNodes;

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
