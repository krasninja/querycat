using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Context for rows output.
/// </summary>
public class RowsOutputQueryContext : QueryContext
{
    private readonly Column[] _columns;

    /// <inheritdoc />
    public override IReadOnlyList<Column> GetColumns() => _columns;

    public RowsOutputQueryContext(Column[] columns)
    {
        _columns = columns;
    }
}
