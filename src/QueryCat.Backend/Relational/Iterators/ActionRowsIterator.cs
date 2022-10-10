using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator executes custom action before and/or after the next row fetch.
/// </summary>
internal sealed class ActionRowsIterator : IRowsIterator
{
    public string Message { get; set; }

    private readonly IRowsIterator _rowsIterator;

    public Action<IRowsIterator>? BeforeMoveNext { get; set; }

    public Action<IRowsIterator>? AfterMoveNext { get; set; }

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public ActionRowsIterator(IRowsIterator rowsIterator, string message)
    {
        Message = message;
        _rowsIterator = rowsIterator;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        BeforeMoveNext?.Invoke(_rowsIterator);
        var result = _rowsIterator.MoveNext();
        if (result)
        {
            AfterMoveNext?.Invoke(_rowsIterator);
        }
        return result;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent(
            $"Action (msg='{Message}' before={BeforeMoveNext != null} after={AfterMoveNext != null})", _rowsIterator);
    }
}
