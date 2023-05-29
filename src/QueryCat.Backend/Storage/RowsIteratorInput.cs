using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Create input from rows iterator.
/// </summary>
public sealed class RowsIteratorInput : IRowsInput, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private readonly string _id;
    private QueryContext _queryContext = new EmptyQueryContext();

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _queryContext;
        set
        {
            _queryContext = value;
            if (!string.IsNullOrEmpty(_id))
            {
                _queryContext.InputInfo.InputArguments = new[] { _id };
            }
        }
    }

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

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Iterator input", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
