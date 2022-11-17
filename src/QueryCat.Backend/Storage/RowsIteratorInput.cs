using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Create input from rows iterator.
/// </summary>
public sealed class RowsIteratorInput : IRowsInput
{
    private readonly IRowsIterator _rowsIterator;
    private QueryContext? _queryContext;
    private readonly string _id;

    /// <inheritdoc />
    public Column[] Columns { get; }

    public RowsIteratorInput(IRowsIterator rowsIterator, string? id = null)
    {
        _rowsIterator = rowsIterator;
        Columns = rowsIterator.Columns;
        _id = id ?? string.Empty;
    }

    /// <inheritdoc />
    public void Open()
    {
    }

    /// <inheritdoc />
    public void SetContext(QueryContext queryContext)
    {
        _queryContext = queryContext;
        if (!string.IsNullOrEmpty(_id))
        {
            queryContext.InputInfo.InputArguments = new[] { _id };
        }
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
        if (_queryContext?.InputInfo != null && !string.IsNullOrEmpty(_id))
        {
            _queryContext.InputInfo.InputArguments = new[] { _id };
        }
        _rowsIterator.Reset();
    }
}
