namespace QueryCat.Backend.Ast.Nodes;

internal sealed class ArrayValuesNode : ListValuesNode
{
    /// <inheritdoc />
    public ArrayValuesNode(IEnumerable<ExpressionNode> valuesNodes) : base(valuesNodes)
    {
    }

    /// <inheritdoc />
    public ArrayValuesNode(ArrayValuesNode node) : base(node)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new ArrayValuesNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
