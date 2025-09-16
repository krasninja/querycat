namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectTableValuesNode : ExpressionNode, ISelectAliasNode, ISelectJoinedNode
{
    public List<SelectTableValuesRowNode> RowsNodes { get; } = new();

    /// <inheritdoc />
    public string Alias { get; set; } = string.Empty;

    /// <inheritdoc />
    public List<SelectTableJoinedNode> JoinedNodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "table_values";

    public SelectTableValuesNode(IEnumerable<SelectTableValuesRowNode> rowsNodes)
    {
        RowsNodes.AddRange(rowsNodes);
    }

    public SelectTableValuesNode(SelectTableValuesNode node)
        : this(node.RowsNodes.Select(n => (SelectTableValuesRowNode)n.Clone()).ToList())
    {
        Alias = node.Alias;
        JoinedNodes.AddRange(node.JoinedNodes.Select(j => (SelectTableJoinedNode)j.Clone()));
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var selectTableValuesRowNode in RowsNodes)
        {
            yield return selectTableValuesRowNode;
        }
        foreach (var selectTableJoinedNode in JoinedNodes)
        {
            yield return selectTableJoinedNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectTableValuesNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
