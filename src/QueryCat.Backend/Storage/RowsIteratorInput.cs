using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Create input from rows iterator.
/// </summary>
public sealed class RowsIteratorInput : IRowsInput
{
    private readonly IRowsIterator _rowsIterator;

    /// <inheritdoc />
    public Column[] Columns { get; }

    public RowsIteratorInput(IRowsIterator rowsIterator)
    {
        _rowsIterator = rowsIterator;
        Columns = rowsIterator.Columns;
    }

    /// <inheritdoc />
    public void Open()
    {
    }

    /// <inheritdoc />
    public void SetContext(QueryContext queryContext)
    {
    }

    /// <inheritdoc />
    public void Close()
    {
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        value = _rowsIterator.Current[columnIndex];
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public bool ReadNext() => _rowsIterator.MoveNext();

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
    }
}
