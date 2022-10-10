namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// In expression array: (1, 2, 3 + 4).
/// </summary>
public sealed class InExpressionValuesNode : ExpressionNode
{
    public IList<ExpressionNode> Values { get; }

    /// <inheritdoc />
    public override string Code => "in_values";

    /// <inheritdoc />
    public InExpressionValuesNode(IList<ExpressionNode> values)
    {
        Values = values;
    }

    public InExpressionValuesNode(InExpressionValuesNode node) :
        this(node.Values.Select(v => (ExpressionNode)v.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var inValue in Values)
        {
            yield return inValue;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new InExpressionValuesNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
