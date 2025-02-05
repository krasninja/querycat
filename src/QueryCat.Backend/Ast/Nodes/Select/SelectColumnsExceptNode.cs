namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectColumnsExceptNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "columns_list_except";

    /// <summary>
    /// Identifiers that should not be shown.
    /// </summary>
    public List<IdentifierExpressionNode> ExceptIdentifiers { get; } = new();

    public SelectColumnsExceptNode(IEnumerable<IdentifierExpressionNode> exceptNodes)
    {
        ExceptIdentifiers.AddRange(exceptNodes);
    }

    public SelectColumnsExceptNode(SelectColumnsExceptNode node)
        : this(node.ExceptIdentifiers.Select(n => (IdentifierExpressionNode)n.Clone()))
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var node in ExceptIdentifiers)
        {
            yield return node;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsExceptNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
