using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast.Nodes;

internal sealed class ArrayValuesNode : ListValuesNode
{
    /// <inheritdoc />
    public override string Code => "array";

    /// <inheritdoc />
    public ArrayValuesNode(IEnumerable<ExpressionNode> valuesNodes) : base(valuesNodes)
    {
        Type = DataType.Array;
    }

    /// <inheritdoc />
    public ArrayValuesNode(ArrayValuesNode node) : base(node)
    {
        Type = DataType.Array;
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new ArrayValuesNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
