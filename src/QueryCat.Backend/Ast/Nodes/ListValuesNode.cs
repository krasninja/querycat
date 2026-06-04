namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// In expression list: (1, 2, 3 + 4).
/// </summary>
internal class ListValuesNode : ExpressionNode
{
    public List<ExpressionNode> ValuesNodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "list";

    /// <inheritdoc />
    public ListValuesNode(IEnumerable<ExpressionNode> valuesNodes)
    {
        ValuesNodes.AddRange(valuesNodes);
    }

    public ListValuesNode(ListValuesNode node) :
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
    public override object Clone() => new ListValuesNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
