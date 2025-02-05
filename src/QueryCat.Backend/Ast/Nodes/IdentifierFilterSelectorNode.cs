namespace QueryCat.Backend.Ast.Nodes;

internal sealed class IdentifierFilterSelectorNode : IdentifierSelectorNode
{
    /// <inheritdoc />
    public override string Code => "id_filter_selector";

    public BinaryOperationExpressionNode FilterExpressionNode { get; }

    /// <inheritdoc />
    public IdentifierFilterSelectorNode(BinaryOperationExpressionNode filterExpressionNode)
    {
        FilterExpressionNode = filterExpressionNode;
    }

    public IdentifierFilterSelectorNode(IdentifierFilterSelectorNode node)
        : this((BinaryOperationExpressionNode)node.FilterExpressionNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return FilterExpressionNode;
    }

    /// <inheritdoc />
    public override object Clone() => new IdentifierFilterSelectorNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => "[?(" + FilterExpressionNode + ")]";
}
