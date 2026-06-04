using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast.Nodes;

internal sealed class MapValuesNode : ExpressionNode
{
    public Dictionary<VariantValue, ExpressionNode> Map { get; } = new();

    /// <inheritdoc />
    public override string Code => "map";

    public MapValuesNode(params ReadOnlySpan<KeyValueNode> keyValueNodes)
    {
        Type = DataType.Map;
        foreach (var keyValueNode in keyValueNodes)
        {
            Map[keyValueNode.Key.Value] = keyValueNode.Value;
        }
    }

    public MapValuesNode(MapValuesNode valuesNode)
    {
        Type = DataType.Map;
        foreach (var keyValue in valuesNode.Map)
        {
            Map.Add(keyValue.Key, keyValue.Value);
        }
        valuesNode.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var keyValue in Map)
        {
            yield return keyValue.Value;
        }
    }

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override object Clone() => new MapValuesNode(this);
}
