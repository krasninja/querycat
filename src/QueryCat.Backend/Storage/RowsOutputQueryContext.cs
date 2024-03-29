using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Context for rows output.
/// </summary>
internal class RowsOutputQueryContext(Column[] columns) : QueryContext
{
    /// <inheritdoc />
    public override QueryContextQueryInfo QueryInfo { get; } = new(columns);
}
