namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectWindowPartitionClauseNode : AstNode
{
    /// <summary>
    /// Partition by expressions.
    /// </summary>
    public List<ExpressionNode> ExpressionNodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "window_partition";

    public SelectWindowPartitionClauseNode(IEnumerable<ExpressionNode> expressionNodes)
    {
        ExpressionNodes.AddRange(expressionNodes);
    }

    public SelectWindowPartitionClauseNode(SelectWindowPartitionClauseNode node)
        : this(node.ExpressionNodes.Select(n => (ExpressionNode)n.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var expressionNode in ExpressionNodes)
        {
            yield return expressionNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectWindowPartitionClauseNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
