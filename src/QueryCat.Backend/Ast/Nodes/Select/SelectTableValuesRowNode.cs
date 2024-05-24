namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectTableValuesRowNode : AstNode
{
    public ExpressionNode[] ExpressionNodes { get; }

    /// <inheritdoc />
    public override string Code => "table_row";

    public SelectTableValuesRowNode(IEnumerable<ExpressionNode> expressionNodes)
    {
        ExpressionNodes = expressionNodes.ToArray();
    }

    public SelectTableValuesRowNode(SelectTableValuesRowNode node)
        : this(node.ExpressionNodes.Select(n => (ExpressionNode)n.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        return ExpressionNodes;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectTableValuesRowNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
