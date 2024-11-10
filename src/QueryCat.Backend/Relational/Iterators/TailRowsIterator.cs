using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator returns the specific number of rows from the source end.
/// </summary>
public sealed class TailRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private readonly int _tailCount;
    private bool _isInitialized;
    private IRowsIterator _currentRowsIterator;

    /// <inheritdoc />
    public Column[] Columns { get; }

    private readonly CacheRowsIterator _cacheRowsIterator;

    /// <inheritdoc />
    public Row Current => _currentRowsIterator.Current;

    public TailRowsIterator(IRowsIterator rowsIterator, int tailCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tailCount, nameof(tailCount));

        _rowsIterator = rowsIterator;
        _tailCount = tailCount;
        _cacheRowsIterator = new CacheRowsIterator(rowsIterator);
        _currentRowsIterator = _cacheRowsIterator;
        Columns = _rowsIterator.Columns;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _cacheRowsIterator.Reset();
        _isInitialized = false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Tail", _cacheRowsIterator);
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        var hasData = _currentRowsIterator.MoveNext();
        if (!hasData && _currentRowsIterator == _cacheRowsIterator)
        {
            _currentRowsIterator = _rowsIterator;
            hasData = _rowsIterator.MoveNext();
        }

        return hasData;
    }

    private void Initialize()
    {
        if (_rowsIterator is ICursorRowsIterator cursorRowsIterator)
        {
            cursorRowsIterator.Seek(-_tailCount, CursorSeekOrigin.End);
            _currentRowsIterator = _rowsIterator;
        }
        else
        {
            while (_rowsIterator.MoveNext())
            {
                _cacheRowsIterator.AddRow(_rowsIterator.Current);
                if (_cacheRowsIterator.TotalRows > _tailCount && _cacheRowsIterator.TotalRows > 0)
                {
                    _cacheRowsIterator.RemoveRowAt(0);
                }
            }
            _cacheRowsIterator.SeekToHead();
        }

        _isInitialized = true;
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
        yield return _cacheRowsIterator;
    }
}
