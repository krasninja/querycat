using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Empty query context.
/// </summary>
public class EmptyQueryContext : QueryContext
{
    public static EmptyQueryContext Empty { get; } = new();

    /// <inheritdoc />
    public override QueryContextQueryInfo QueryInfo { get; } = new(
        columns: Array.Empty<Column>(),
        limit: null);
}
