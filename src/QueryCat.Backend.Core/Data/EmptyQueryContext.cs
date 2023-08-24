namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Empty query context.
/// </summary>
public class NullQueryContext : QueryContext
{
    public static NullQueryContext Instance { get; } = new();

    /// <inheritdoc />
    public override QueryContextQueryInfo QueryInfo { get; } = new(
        columns: Array.Empty<Column>(),
        limit: null);
}
