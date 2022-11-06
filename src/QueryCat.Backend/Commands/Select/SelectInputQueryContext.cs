using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// Context for input rows source.
/// </summary>
public class SelectInputQueryContext : QueryContext
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
        InputInfo = new QueryContextInputInfo(rowsInput);
        QueryInfo = new QueryContextQueryInfo(rowsInput.Columns);
    }
}
