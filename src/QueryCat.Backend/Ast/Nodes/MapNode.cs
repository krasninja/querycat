using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast.Nodes;

internal sealed class MapNode : ExpressionNode
{
    public Dictionary<VariantValue, ExpressionNode> Map { get; } = new();

    /// <inheritdoc />
    public override string Code => "map";

    public MapNode(params ReadOnlySpan<KeyValueNode> keyValueNodes)
    {
        foreach (var keyValueNode in keyValueNodes)
        {
            Map[keyValueNode.Key.Value] = keyValueNode.Value;
        }
    }

    public MapNode(MapNode node)
    {
        foreach (var keyValue in node.Map)
        {
            Map.Add(keyValue.Key, keyValue.Value);
        }
        node.CopyTo(this);
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
    public override object Clone() => new MapNode(this);
}
