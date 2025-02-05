namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// In expression array: (1, 2, 3 + 4).
/// </summary>
internal sealed class InExpressionValuesNode : ExpressionNode
{
    public List<ExpressionNode> ValuesNodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "in_values";

    /// <inheritdoc />
    public InExpressionValuesNode(IEnumerable<ExpressionNode> valuesNodes)
    {
        ValuesNodes.AddRange(valuesNodes);
    }

    public InExpressionValuesNode(InExpressionValuesNode node) :
        this(node.ValuesNodes.Select(v => (ExpressionNode)v.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var inValue in ValuesNodes)
        {
            yield return inValue;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new InExpressionValuesNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
