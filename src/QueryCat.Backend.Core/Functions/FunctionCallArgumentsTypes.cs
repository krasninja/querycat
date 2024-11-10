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

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="positional">Positional arguments types.</param>
    /// <param name="namedArguments">Named arguments types.</param>
    public FunctionCallArgumentsTypes(
        KeyValuePair<int, DataType>[]? positional = null,
        KeyValuePair<string, DataType>[]? namedArguments = null)
    {
        Positional = positional ?? [];
        Named = namedArguments ?? [];
    }

    /// <summary>
    /// Create instance of <see cref="FunctionCallArgumentsTypes"/> from
    /// only positional arguments types.
    /// </summary>
    /// <param name="positional">Positional types.</param>
    /// <returns>Instance of <see cref="FunctionCallArgumentsTypes" />.</returns>
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
