using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

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

    /// <inheritdoc />
    public override string ToString() => FunctionFormatter.ValueToString(this.Value);
}
