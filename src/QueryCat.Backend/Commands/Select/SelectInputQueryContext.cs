using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

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
    public SelectInputQueryContext(IRowsInput rowsInput)
    {
        RowsInput = rowsInput;
    }

    /// <inheritdoc />
    public override IReadOnlyList<Column> GetColumns() => RowsInput.Columns;
}
