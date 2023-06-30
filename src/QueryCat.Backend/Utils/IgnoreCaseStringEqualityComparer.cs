namespace QueryCat.Backend.Utils;

/// <summary>
/// Case insensitive equality comparer.
/// </summary>
public sealed class IgnoreCaseStringEqualityComparer : IEqualityComparer<string>
{
    public static IgnoreCaseStringEqualityComparer Instance { get; } = new();

    /// <inheritdoc />
    public bool Equals(string? x, string? y)
    {
        if (x == null || y == null)
        {
            return false;
        }
        return x.Equals(y, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public int GetHashCode(string obj) => obj.GetHashCode();
}
