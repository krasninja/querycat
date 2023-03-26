using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Context for rows output.
/// </summary>
public class RowsOutputQueryContext : QueryContext
{
    /// <inheritdoc />
    public override QueryContextQueryInfo QueryInfo { get; }

    public RowsOutputQueryContext(Column[] columns, IExecutionThread executionThread)
        : base(executionThread)
    {
        QueryInfo = new QueryContextQueryInfo(columns);
    }
}
