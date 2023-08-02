using QueryCat.Backend.Abstractions;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Context for rows output.
/// </summary>
public class RowsOutputQueryContext : QueryContext
{
    /// <inheritdoc />
    public override QueryContextQueryInfo QueryInfo { get; }

    public RowsOutputQueryContext(Column[] columns)
    {
        QueryInfo = new QueryContextQueryInfo(columns);
    }
}
