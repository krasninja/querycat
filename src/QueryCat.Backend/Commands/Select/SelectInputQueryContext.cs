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
    public SelectInputQueryContext(IRowsInput rowsInput, Column[] columns)
    {
        RowsInput = rowsInput;
        QueryInfo = new QueryContextQueryInfo(columns);
    }

    /// <inheritdoc />
    public SelectInputQueryContext(IRowsInput rowsInput) : this(rowsInput, rowsInput.Columns)
    {
    }
}
