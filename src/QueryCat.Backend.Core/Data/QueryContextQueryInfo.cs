using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// The class contains information about query - selected columns, conditions.
/// </summary>
public sealed class QueryContextQueryInfo
{
    /// <summary>
    /// Get columns for select.
    /// </summary>
    public Column[] Columns { get; }

    /// <summary>
    /// Rows offset.
    /// </summary>
    public long Offset { get; internal set; }

    /// <summary>
    /// Get rows limit. Allows to select only needed amount of records.
    /// </summary>
    public long? Limit { get; internal set; }

    /// <summary>
    /// Extra parameters passed to the rows source. Can be used to provide
    /// additional information about the executing query.
    /// </summary>
    public IReadOnlyDictionary<string, VariantValue> ExtraParams { get; init; } = new Dictionary<string, VariantValue>();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columns">Select columns.</param>
    /// <param name="limit">Select limit.</param>
    public QueryContextQueryInfo(IReadOnlyList<Column> columns, long? limit = null)
    {
        Columns = columns.ToArray();
        Limit = limit;
    }
}
