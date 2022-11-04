using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Empty query context.
/// </summary>
public class EmptyQueryContext : QueryContext
{
    public static EmptyQueryContext Empty { get; } = new();

    /// <inheritdoc />
    public override IReadOnlyList<Column> GetColumns() => Array.Empty<Column>();

    /// <inheritdoc />
    internal override CacheKey GetCacheKey() => CacheKey.Empty;
}
