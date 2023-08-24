using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// The class contains extracted function arguments types separated
/// by positional and named.
/// </summary>
public sealed class FunctionCallArgumentsTypes
{
    /// <summary>
    /// Positional arguments.
    /// </summary>
    public KeyValuePair<int, DataType>[] Positional { get; }

    /// <summary>
    /// Named arguments.
    /// </summary>
    public KeyValuePair<string, DataType>[] Named { get; }

    /// <summary>
    /// Total count of arguments.
    /// </summary>
    public int TotalCount => Positional.Length + Named.Length;

    public FunctionCallArgumentsTypes(
        KeyValuePair<int, DataType>[]? positional = null,
        KeyValuePair<string, DataType>[]? namedArguments = null)
    {
        Positional = positional ?? Array.Empty<KeyValuePair<int, DataType>>();
        Named = namedArguments ?? Array.Empty<KeyValuePair<string, DataType>>();
    }

    public static FunctionCallArgumentsTypes FromPositionArguments(
        params DataType[] positional)
    {
        return new FunctionCallArgumentsTypes(
            positional.Select((arg, pos) =>
                new KeyValuePair<int, DataType>(pos, arg))
                    .ToArray());
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var positionalArguments = Positional.Select(a => a.Value.ToString());
        var namedArguments = Named.Select(a => string.Join(", ", string.Concat(a.Key, "=", a.Value)));
        return string.Join(", ", positionalArguments.Concat(namedArguments));
    }
}
