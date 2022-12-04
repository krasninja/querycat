namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectColumnsSublistExpressionNode : SelectColumnsSublistNode
{
    public ExpressionNode ExpressionNode { get; }

    /// <inheritdoc />
    public override string Code => "column_expr";

    /// <inheritdoc />
    public SelectColumnsSublistExpressionNode(ExpressionNode expressionNode)
    {
        ExpressionNode = expressionNode;
    }

    public SelectColumnsSublistExpressionNode(SelectColumnsSublistExpressionNode node) :
        this((ExpressionNode)node.ExpressionNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ExpressionNode;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsSublistExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => ExpressionNode.ToString() ?? string.Empty;
}
