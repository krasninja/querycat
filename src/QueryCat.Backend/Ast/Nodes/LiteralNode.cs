using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Terminal.
/// </summary>
public sealed class LiteralNode : ExpressionNode
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

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
