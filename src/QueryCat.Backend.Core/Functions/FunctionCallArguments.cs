using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Utils;

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
    /// Create from query string. Example string: arg1=10&amp;Name=John.
    /// </summary>
    /// <param name="query">Query.</param>
    /// <returns>Instance of <see cref="FunctionCallArguments" />.</returns>
    public static FunctionCallArguments FromQueryString(string query)
    {
        var args = StringUtils.GetFieldsFromLine(query, delimiter: '&');
        var fa = new FunctionCallArguments();
        if (args.Length == 1 && args[0].IndexOf('=') == -1)
        {
            fa.Add(CreateValueFromString(args[0]));
        }
        else
        {
            foreach (var arg in args)
            {
                var delimiterIndex = arg.IndexOf('=');
                if (delimiterIndex == -1)
                {
                    continue;
                }
                var name = arg.Substring(0, delimiterIndex);
                var value = CreateValueFromString(arg.Substring(delimiterIndex + 1));
                fa.Add(name, value);
            }
        }
        return fa;
    }

    private static VariantValue CreateValueFromString(string str)
    {
        var type = DataTypeUtils.DetermineTypeByValue(str);
        if (type == DataType.String)
        {
            var stringValue = StringUtils.Unquote(str);
            stringValue = StringUtils.Unquote(stringValue, quoteChar: "'");
            return new VariantValue(StringUtils.Unescape(stringValue.ToString()));
        }
        if (VariantValue.TryCreateFromString(str, type, out var value))
        {
            return value;
        }
        throw new InvalidOperationException($"Cannot parse value '{str}'.");
    }

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
            _positional.Select((p, i) => new KeyValuePair<int, DataType>(i, p.GetInternalType())).ToArray(),
            _named.Select(p => new KeyValuePair<string, DataType>(p.Key, p.Value.GetInternalType())).ToArray()
        );
    }
}
