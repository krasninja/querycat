using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// Read the rows iterator and cache its data. If it goes above max cache size - read
/// rows directly from original rows iterator.
/// </summary>
public sealed class CacheRowsIterator : ICursorRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private readonly int _cacheSize;
    private readonly List<Row> _cache;
    private int _cursor = -1;
    private Row _currentRow;
    private bool _isFrozen;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public int Position => _cursor;

    /// <inheritdoc />
    public int TotalRows => _cache.Count;

    /// <inheritdoc />
    public Row Current => _currentRow;

    /// <summary>
    /// Is the current cache cursor position at the last row.
    /// </summary>
    public bool EndOfCache => _isFrozen && Position >= _cache.Count;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rowsIterator">Row iterator.</param>
    /// <param name="cacheSize">Max cache size. -1 for no limit.</param>
    public CacheRowsIterator(IRowsIterator rowsIterator, int cacheSize = -1)
    {
        _currentRow = new Row(rowsIterator); // Empty.
        _rowsIterator = rowsIterator;
        _cacheSize = cacheSize;
        _cache = new List<Row>(_cacheSize > 0 ? _cacheSize : 32);
    }

    /// <summary>
    /// Add row manually to the cache. It ignores the max cache limit.
    /// </summary>
    /// <param name="row">Row to add.</param>
    /// <returns><c>True</c> if row has been added, <c>false</c> otherwise.</returns>
    public bool AddRow(Row row)
    {
        if (_isFrozen)
        {
            return false;
        }
        _cache.Add(new Row(row));
        return true;
    }

    /// <summary>
    /// Remove row at specified index.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    public void RemoveRowAt(int rowIndex)
    {
        _cache.RemoveAt(rowIndex);
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        // If our position within the cache - return cached data.
        if (_cursor + 1 <= _cache.Count - 1)
        {
            _cursor++;
            _currentRow = _cache[_cursor];
            return true;
        }

        if (_isFrozen)
        {
            return false;
        }

        var hasData = await _rowsIterator.MoveNextAsync(cancellationToken);
        if (!hasData)
        {
            return false;
        }

        // Move next and add to cache.
        _currentRow = _rowsIterator.Current;
        if (_cursor < _cacheSize || _cacheSize == -1)
        {
            _cache.Add(new Row(_currentRow));
        }
        _cursor++;
        return true;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _rowsIterator.ResetAsync(cancellationToken);
        _cache.Clear();
        _cursor = -1;
    }

    /// <summary>
    /// Move cursor position to the beginning of data.
    /// </summary>
    public void SeekToHead() => Seek(-1, CursorSeekOrigin.Begin);

    /// <inheritdoc />
    public void Seek(int offset, CursorSeekOrigin origin)
    {
        if (origin == CursorSeekOrigin.Begin)
        {
            _cursor = offset;
        }
        else if (origin == CursorSeekOrigin.Current)
        {
            _cursor += offset;
        }
        else if (origin == CursorSeekOrigin.End)
        {
            _cursor = TotalRows - offset;
        }
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Cache (max={_cacheSize} fill={_cache.Count})", _rowsIterator);
    }

    /// <summary>
    /// Freezes the cache. In this mode new rows cannot be added.
    /// </summary>
    public void Freeze()
    {
        _isFrozen = true;
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
