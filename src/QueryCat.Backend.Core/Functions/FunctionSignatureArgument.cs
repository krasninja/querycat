using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Describes function signature argument.
/// </summary>
public sealed class FunctionSignatureArgument : ICloneable
{
    /// <summary>
    /// Argument name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Argument type.
    /// </summary>
    public DataType Type { get; }

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

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Argument name.</param>
    /// <param name="type">Argument type.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="isOptional">Is optional argument.</param>
    /// <param name="isArray">Is array argument.</param>
    /// <param name="isVariadic">Is variadic. Allows a function to accept any number of extra arguments.</param>
    public FunctionSignatureArgument(
        string name,
        DataType type,
        VariantValue? defaultValue = null,
        bool isOptional = false,
        bool isArray = false,
        bool isVariadic = false)
    {
        Name = name;
        Type = type;
        IsOptional = isOptional;
        HasDefaultValue = defaultValue != null;
        DefaultValue = defaultValue ?? VariantValue.Null;
        IsArray = isArray;
        IsVariadic = isVariadic;
        if (IsVariadic && !IsArray)
        {
            throw new ArgumentException(Resources.Errors.VariadicMustBeArray, nameof(isVariadic));
        }
        if (IsOptional && !HasDefaultValue)
        {
            DefaultValue = VariantValue.Null;
            HasDefaultValue = true;
        }
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="argument">Instance of <see cref="FunctionSignatureArgument" />.</param>
    public FunctionSignatureArgument(FunctionSignatureArgument argument)
        : this(
            argument.Name,
            argument.Type,
            argument.HasDefaultValue ? argument.DefaultValue : null,
            argument.IsOptional,
            argument.IsArray,
            argument.IsVariadic)
    {
    }

    /// <inheritdoc />
    public object Clone() => new FunctionSignatureArgument(this);

    /// <inheritdoc />
    public override string ToString() => $"{Name}: {Type}";
}
