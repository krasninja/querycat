using System.Text;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast.Nodes.Function;

/// <summary>
/// Function argument.
/// </summary>
public sealed class FunctionSignatureArgumentNode : AstNode, IEquatable<FunctionSignatureArgumentNode>
{
    /// <summary>
    /// Argument name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Argument type.
    /// </summary>
    public FunctionTypeNode TypeNode { get; }

    /// <summary>
    /// The argument is optional.
    /// </summary>
    public bool IsOptional { get; }

    /// <summary>
    /// Default argument value (if it was not set).
    /// </summary>
    public VariantValue DefaultValue { get; }

    /// <summary>
    /// Returns <c>true</c> if the argument has default value, <c>false</c> otherwise.
    /// </summary>
    public bool HasDefaultValue { get; }

    /// <summary>
    /// Is the argument array. It can be only the last argument.
    /// </summary>
    public bool IsArray { get; }

    /// <summary>
    /// The function accepts variable number of arguments. There can be only one last variadic argument.
    /// </summary>
    public bool IsVariadic { get; }

    /// <inheritdoc />
    public override string Code => "func_arg";

    public FunctionSignatureArgumentNode(
        string name,
        FunctionTypeNode typeNode,
        VariantValue? defaultValue = null,
        bool isOptional = false,
        bool isArray = false,
        bool isVariadic = false)
    {
        Name = name;
        TypeNode = typeNode;
        IsOptional = isOptional;
        HasDefaultValue = defaultValue != null;
        DefaultValue = defaultValue ?? VariantValue.Null;
        IsArray = isArray;
        IsVariadic = isVariadic;
        if (IsVariadic && !IsArray)
        {
            throw new ArgumentException("Variadic type must be array.", nameof(isVariadic));
        }
        if (IsOptional && !HasDefaultValue)
        {
            DefaultValue = VariantValue.Null;
            HasDefaultValue = true;
        }
    }

    public FunctionSignatureArgumentNode(FunctionSignatureArgumentNode node) :
        this(node.Name, node.TypeNode, node.DefaultValue, node.IsOptional, node.IsArray, node.IsVariadic)
    {
        HasDefaultValue = node.HasDefaultValue;
        DefaultValue = node.DefaultValue;
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override object Clone() => new FunctionSignatureArgumentNode(this);

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder(65);
        if (IsVariadic)
        {
            sb.Append("...");
        }
        sb.Append(Name);
        sb.Append(": ");
        sb.Append(TypeNode);
        if (HasDefaultValue && !DefaultValue.IsNull)
        {
            sb.Append($"={DefaultValue}");
        }
        return sb.ToString();
    }

    /// <inheritdoc />
    public bool Equals(FunctionSignatureArgumentNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return Name == other.Name
            && TypeNode.Equals(other.TypeNode)
            && IsOptional == other.IsOptional
            && DefaultValue.Equals(other.DefaultValue)
            && HasDefaultValue == other.HasDefaultValue
            && IsArray == other.IsArray
            && IsVariadic == other.IsVariadic;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is FunctionSignatureArgumentNode other && Equals(other));

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Name, TypeNode, IsOptional, DefaultValue, HasDefaultValue, IsArray, IsVariadic);

    public static bool operator ==(FunctionSignatureArgumentNode? left, FunctionSignatureArgumentNode? right)
        => Equals(left, right);

    public static bool operator !=(FunctionSignatureArgumentNode? left, FunctionSignatureArgumentNode? right)
        => !Equals(left, right);
}
