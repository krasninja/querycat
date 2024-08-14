using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Terminal.
/// </summary>
internal sealed class LiteralNode : ExpressionNode
{
    /// <summary>
    /// Literal value.
    /// </summary>
    public VariantValue Value { get; }

    /// <inheritdoc />
    public override string Code => "literal";

    public static LiteralNode NullValueNode { get; } = new(VariantValue.Null);

    public LiteralNode(VariantValue value)
    {
        Value = value;
    }

    public LiteralNode(string value)
    {
        Value = new VariantValue(value);
    }

    public LiteralNode(LiteralNode node) : this(node.Value)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override object Clone() => new LiteralNode(this);

    private static string Quote(string target) => StringUtils.Quote(target, quote: "\'").ToString();

    internal static string ValueToString(VariantValue value) => value.GetInternalType() switch
    {
        DataType.String => Quote(value.AsStringUnsafe),
        DataType.Timestamp => Quote(value.AsTimestampUnsafe.ToString(Application.Culture)) + "::timestamp",
        DataType.Interval => Quote(value.AsIntervalUnsafe.ToString("c", Application.Culture)) + "::interval",
        DataType.Object => Quote($"[object:{value.AsObjectUnsafe?.GetType().Name}]"),
        DataType.Blob => "E" + Quote(value.ToString()),
        _ => value.ToString(),
    };

    /// <inheritdoc />
    public override string ToString() => ValueToString(this.Value);
}
