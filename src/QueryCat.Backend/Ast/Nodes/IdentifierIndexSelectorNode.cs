namespace QueryCat.Backend.Ast.Nodes;

internal sealed class IdentifierIndexSelectorNode : IdentifierSelectorNode
{
    /// <inheritdoc />
    public override string Code => "id_index_selector";

    public ExpressionNode[] IndexExpressions { get; }

    /// <inheritdoc />
    public IdentifierIndexSelectorNode(List<ExpressionNode> indexExpression)
    {
        IndexExpressions = indexExpression.ToArray();
    }

    public IdentifierIndexSelectorNode(IdentifierIndexSelectorNode node)
        : this(node.IndexExpressions.Select(e => (ExpressionNode)e.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var indexExpression in IndexExpressions)
        {
            yield return indexExpression;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new IdentifierIndexSelectorNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString()
        => "[" + string.Join(", ", IndexExpressions.Select(e => e.ToString()).ToArray()) + "]";
}
