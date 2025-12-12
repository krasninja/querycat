using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// The class contains extracted function arguments types separated
/// by positional and named.
/// </summary>
public sealed class FunctionCallArgumentsTypes : IEqualityComparer<FunctionCallArgumentsTypes>, IEquatable<FunctionCallArgumentsTypes>
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
    /// <param name="named">Named arguments types.</param>
    public FunctionCallArgumentsTypes(
        KeyValuePair<int, DataType>[]? positional = null,
        KeyValuePair<string, DataType>[]? named = null)
    {
        Positional = positional ?? [];
        Named = named ?? [];
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
    public bool Equals(FunctionCallArgumentsTypes? x, FunctionCallArgumentsTypes? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }
        if (x is null)
        {
            return false;
        }
        if (y is null)
        {
            return false;
        }
        if (x.GetType() != y.GetType())
        {
            return false;
        }
        if (x.Positional.Length != y.Positional.Length
            || x.Named.Length != y.Named.Length)
        {
            return false;
        }

        for (var i = 0; i < x.Positional.Length; i++)
        {
            if (x.Positional[i].Key != y.Positional[i].Key)
            {
                return false;
            }
        }
        for (var i = 0; i < x.Named.Length; i++)
        {
            if (x.Named[i].Key != y.Named[i].Key)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public bool Equals(FunctionCallArgumentsTypes? other) => Equals(this, other);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is FunctionCallArgumentsTypes other && Equals(other));

    /// <inheritdoc />
    public int GetHashCode(FunctionCallArgumentsTypes obj)
    {
        var hashCode = default(HashCode);
        foreach (var pair in Positional)
        {
            hashCode.Add(pair.Key);
            hashCode.Add(pair.Value);
        }
        foreach (var pair in Named)
        {
            hashCode.Add(pair.Key);
            hashCode.Add(pair.Value);
        }
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public override int GetHashCode() => GetHashCode(this);

    /// <inheritdoc />
    public override string ToString()
    {
        var positionalArguments = Positional.Select(a => a.Value.ToString());
        var namedArguments = Named.Select(a => string.Join(", ", string.Concat(a.Key, "=", a.Value)));
        return string.Join(", ", positionalArguments.Concat(namedArguments));
    }
}
