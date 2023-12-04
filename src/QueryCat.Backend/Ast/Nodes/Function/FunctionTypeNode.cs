using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast.Nodes.Function;

internal sealed class FunctionTypeNode : TypeNode, IEquatable<FunctionTypeNode>
{
    public static FunctionTypeNode NullTypeInstance { get; } = new(DataType.Null);

    public string TypeName { get; }

    /// <inheritdoc />
    public FunctionTypeNode(DataType type, string? typeName = null) : base(type)
    {
        TypeName = typeName ?? string.Empty;
    }

    /// <inheritdoc />
    public FunctionTypeNode(FunctionTypeNode node) : this(node.Type, node.TypeName)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new FunctionTypeNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => string.IsNullOrEmpty(TypeName)
        ? Type.ToString()
        : $"{Type}<{TypeName}>";

    /// <inheritdoc />
    public bool Equals(FunctionTypeNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return TypeName == other.TypeName;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is FunctionTypeNode other && Equals(other));

    /// <inheritdoc />
    public override int GetHashCode()
        => TypeName.GetHashCode();

    public static bool operator ==(FunctionTypeNode? left, FunctionTypeNode? right)
        => Equals(left, right);

    public static bool operator !=(FunctionTypeNode? left, FunctionTypeNode? right)
        => !Equals(left, right);
}
