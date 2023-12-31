using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator limits the number of returned rows.
/// </summary>
internal sealed class LimitRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly long _limit;
    private readonly IRowsIterator _rowsIterator;
    private long _count;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public LimitRowsIterator(IRowsIterator rowsIterator, long limit)
    {
        _rowsIterator = rowsIterator;
        _limit = limit;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (_count >= _limit)
        {
            return false;
        }
        _count++;
        return _rowsIterator.MoveNext();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
        _count = 0;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Limit (row={_limit})", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
