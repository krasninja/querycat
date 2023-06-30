using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The class contains information about query - selected columns, conditions.
/// </summary>
public sealed class QueryContextQueryInfo
{
    /// <summary>
    /// Get columns for select.
    /// </summary>
    public IReadOnlyList<Column> Columns { get; internal set; }

    /// <summary>
    /// Rows offset.
    /// </summary>
    public long Offset { get; internal set; }

    /// <summary>
    /// Get rows limit. Allows to select only needed amount of records.
    /// </summary>
    public long? Limit { get; internal set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columns">Select columns.</param>
    /// <param name="limit">Select limit.</param>
    public QueryContextQueryInfo(IReadOnlyList<Column> columns, long? limit = null)
    {
        Columns = columns;
        Limit = limit;
    }
}
