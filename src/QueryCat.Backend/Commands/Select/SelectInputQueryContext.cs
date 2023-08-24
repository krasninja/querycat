using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// Context for input rows source.
/// </summary>
internal class SelectInputQueryContext : QueryContext
{
    /// <summary>
    /// The target rows input.
    /// </summary>
    public IRowsInput RowsInput { get; }

    /// <inheritdoc />
    public override QueryContextQueryInfo QueryInfo { get; }

    /// <inheritdoc />
    public SelectInputQueryContext(IRowsInput rowsInput)
    {
        RowsInput = rowsInput;
        QueryInfo = new QueryContextQueryInfo(rowsInput.Columns);
    }
}
