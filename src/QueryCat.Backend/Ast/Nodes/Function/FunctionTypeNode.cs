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
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

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
        return Type == other.Type && TypeName == other.TypeName;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is FunctionTypeNode other && Equals(other));

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Type);
        hash.Add(TypeName);
        return hash.ToHashCode();
    }

    public static bool operator ==(FunctionTypeNode? left, FunctionTypeNode? right)
        => Equals(left, right);

    public static bool operator !=(FunctionTypeNode? left, FunctionTypeNode? right)
        => !Equals(left, right);
}
