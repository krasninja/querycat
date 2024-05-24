namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectTableValuesNode : ExpressionNode, ISelectAliasNode
{
    public List<SelectTableValuesRowNode> RowsNodes { get; } = new();

    /// <inheritdoc />
    public string Alias { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public override string Code => "table_values";

    public SelectTableValuesNode(IEnumerable<SelectTableValuesRowNode> rowsNodes)
    {
        RowsNodes.AddRange(rowsNodes);
    }

    public SelectTableValuesNode(SelectTableValuesNode valuesNode)
        : this(valuesNode.RowsNodes.Select(n => (SelectTableValuesRowNode)n.Clone()).ToList())
    {
        valuesNode.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        return RowsNodes;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectTableValuesNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
