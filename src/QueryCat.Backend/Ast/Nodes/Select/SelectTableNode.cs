namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectTableNode : ExpressionNode, ISelectAliasNode
{
    public List<SelectTableRowNode> RowsNodes { get; } = new();

    /// <inheritdoc />
    public string Alias { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public override string Code => "table_value";

    public SelectTableNode(IEnumerable<SelectTableRowNode> rowsNodes)
    {
        RowsNodes.AddRange(rowsNodes);
    }

    public SelectTableNode(SelectTableNode node)
        : this(node.RowsNodes.Select(n => (SelectTableRowNode)n.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        return RowsNodes;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectTableNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
