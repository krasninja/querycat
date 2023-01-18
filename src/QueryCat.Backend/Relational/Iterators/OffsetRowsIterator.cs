using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator makes offset in rows reading.
/// </summary>
internal sealed class OffsetRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly long _offset;
    private readonly IRowsIterator _rowsIterator;
    private long _count;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public OffsetRowsIterator(IRowsIterator rowsIterator, long offset)
    {
        _rowsIterator = rowsIterator;
        _offset = offset;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        while (_offset > _count && _rowsIterator.MoveNext())
        {
            _count++;
        }
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
        stringBuilder.AppendRowsIteratorsWithIndent($"Offset (rows={_offset})", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
