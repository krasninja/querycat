namespace QueryCat.Backend.Ast.Nodes;

internal sealed class IdentifierIndexSelectorNode : IdentifierSelectorNode
{
    /// <inheritdoc />
    public override string Code => "id_index_selector";

    public ExpressionNode IndexExpression { get; }

    /// <inheritdoc />
    public IdentifierIndexSelectorNode(ExpressionNode indexExpression)
    {
        IndexExpression = indexExpression;
    }

    public IdentifierIndexSelectorNode(IdentifierIndexSelectorNode node)
        : this((ExpressionNode)node.IndexExpression.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new IdentifierIndexSelectorNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
