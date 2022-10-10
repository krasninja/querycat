using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator executes tear down delegates after all rows processing.
/// </summary>
public class TearDownRowsIterator : IRowsIterator
{
    private readonly string _message;
    private readonly IRowsIterator _rowsIterator;
    private bool _isExecuted;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public Action<IRowsIterator> Action { get; set; } = _ => { };

    public TearDownRowsIterator(IRowsIterator rowsIterator, string message)
    {
        _message = message;
        _rowsIterator = rowsIterator;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        var hasData = _rowsIterator.MoveNext();
        if (!hasData && !_isExecuted)
        {
            Action.Invoke(_rowsIterator);
            _isExecuted = true;
        }
        return hasData;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
        _isExecuted = false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"TearDown (msg='{_message}')", _rowsIterator);
    }
}
