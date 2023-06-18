using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Represents the input arguments for function.
/// </summary>
public sealed class FunctionArguments
{
    private readonly Dictionary<string, VariantValue> _named = new();
    private readonly List<VariantValue> _positional = new();

    /// <summary>
    /// Named arguments.
    /// </summary>
    public IReadOnlyDictionary<string, VariantValue> Named => _named;

    /// <summary>
    /// Positional arguments.
    /// </summary>
    public IReadOnlyList<VariantValue> Positional => _positional;

    /// <summary>
    /// Create instance.
    /// </summary>
    /// <returns>Instance of <see cref="FunctionArguments" />.</returns>
    public static FunctionArguments Create() => new();

    /// <summary>
    /// Add named argument.
    /// </summary>
    /// <param name="name">Argument name.</param>
    /// <param name="value">Argument value.</param>
    /// <returns>Instance of <see cref="FunctionArguments" />.</returns>
    public FunctionArguments Add(string name, VariantValue value)
    {
        _named[name] = value;
        return this;
    }

    /// <summary>
    /// Add named argument.
    /// </summary>
    /// <param name="name">Argument name.</param>
    /// <param name="value">Argument value.</param>
    /// <typeparam name="T">Argument type.</typeparam>
    /// <returns>Instance of <see cref="FunctionArguments" />.</returns>
    public FunctionArguments Add<T>(string name, T value)
    {
        _named[name] = VariantValue.CreateFromObject(value);
        return this;
    }

    /// <summary>
    /// Add named argument.
    /// </summary>
    /// <param name="value">Argument value.</param>
    /// <returns>Instance of <see cref="FunctionArguments" />.</returns>
    public FunctionArguments Add(VariantValue value)
    {
        _positional.Add(value);
        return this;
    }

    /// <summary>
    /// Add named argument.
    /// </summary
    /// <param name="value">Argument value.</param>
    /// <typeparam name="T">Argument type.</typeparam>
    /// <returns>Instance of <see cref="FunctionArguments" />.</returns>
    public FunctionArguments Add<T>(T value)
    {
        _positional.Add(VariantValue.CreateFromObject(value));
        return this;
    }

    public FunctionArgumentsTypes GetTypes()
    {
        return new FunctionArgumentsTypes(
            _positional.Select((p, i) => new KeyValuePair<int, DataType>(i, p.GetInternalType())).ToArray(),
            _named.Select(p => new KeyValuePair<string, DataType>(p.Key, p.Value.GetInternalType())).ToArray()
        );
    }
}
