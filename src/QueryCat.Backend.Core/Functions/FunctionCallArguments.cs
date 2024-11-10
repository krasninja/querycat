using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Represents the input arguments for function.
/// </summary>
public sealed class FunctionCallArguments
{
    private readonly Dictionary<string, VariantValue> _named = new();
    private readonly List<VariantValue> _positional = new();

    internal static FunctionCallArguments Empty { get; } = new();

    /// <summary>
    /// Named arguments.
    /// </summary>
    public IReadOnlyDictionary<string, VariantValue> Named => _named;

    /// <summary>
    /// Positional arguments.
    /// </summary>
    public IReadOnlyList<VariantValue> Positional => _positional;

    /// <summary>
    /// Add named argument.
    /// </summary>
    /// <param name="name">Argument name.</param>
    /// <param name="value">Argument value.</param>
    /// <param name="overwrite">Overwrite if arguments with the same name exists.</param>
    /// <returns>Instance of <see cref="FunctionCallArguments" />.</returns>
    public FunctionCallArguments Add(string name, VariantValue value, bool overwrite = false)
    {
        name = name.ToUpper();
        if (overwrite || !_named.ContainsKey(name))
        {
            _named[name] = value;
        }
        return this;
    }

    /// <summary>
    /// Add named argument.
    /// </summary>
    /// <param name="name">Argument name.</param>
    /// <param name="value">Argument value.</param>
    /// <param name="overwrite">Overwrite if arguments with the same name exists.</param>
    /// <typeparam name="T">Argument type.</typeparam>
    /// <returns>Instance of <see cref="FunctionCallArguments" />.</returns>
    public FunctionCallArguments Add<T>(string name, T value, bool overwrite = false)
    {
        name = name.ToUpper();
        if (overwrite || !_named.ContainsKey(name))
        {
            _named[name] = VariantValue.CreateFromObject(value);
        }
        return this;
    }

    /// <summary>
    /// Add named argument.
    /// </summary>
    /// <param name="value">Argument value.</param>
    /// <returns>Instance of <see cref="FunctionCallArguments" />.</returns>
    public FunctionCallArguments Add(VariantValue value)
    {
        _positional.Add(value);
        return this;
    }

    /// <summary>
    /// Add named argument.
    /// </summary>
    /// <param name="value">Argument value.</param>
    /// <typeparam name="T">Argument type.</typeparam>
    /// <returns>Instance of <see cref="FunctionCallArguments" />.</returns>
    public FunctionCallArguments Add<T>(T value)
    {
        _positional.Add(VariantValue.CreateFromObject(value));
        return this;
    }

    public FunctionCallArgumentsTypes GetTypes()
    {
        return new FunctionCallArgumentsTypes(
            _positional.Select((p, i) => new KeyValuePair<int, DataType>(i, p.Type)).ToArray(),
            _named.Select(p => new KeyValuePair<string, DataType>(p.Key, p.Value.Type)).ToArray()
        );
    }
}
