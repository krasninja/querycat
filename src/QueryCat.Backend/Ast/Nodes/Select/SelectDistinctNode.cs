namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectDistinctNode : AstNode
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

    public IList<ExpressionNode> On { get; } = new List<ExpressionNode>();

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

    public SelectDistinctNode(IList<ExpressionNode> on)
    {
        On = on;
    }

    public SelectDistinctNode(SelectDistinctNode node) :
        this(node.On.Select(g => (ExpressionNode)g.Clone()).ToList())
    {
        node.IsAll = this.IsAll;
        node.IsEmpty = this.IsEmpty;
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectDistinctNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => On;

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
