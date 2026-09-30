using System.Diagnostics;
using System.Globalization;
using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// Read the rows iterator and cache its data. If it goes above max cache size - read
/// rows directly from original rows iterator.
/// </summary>
[DebuggerDisplay("Position = {Position}, Count = {Count}")]
public sealed class CacheRowsIterator : IRowsIteratorParent, ICursorRowsIterator
{
    private const int InitialPosition = -1;

    private readonly IRowsIterator _rowsIterator;
    private int _rowsIteratorCursor = InitialPosition; // The index of last row read.
    private readonly int _cacheSize;
    private List<Row> _cache;
    private int _cursor = InitialPosition; // Absolute cursor position, might be within cache of rows iterator.
    private Row _currentRow;
    private bool _isFrozen;

    private readonly TimeSpan _expiresIn;
    private DateTime _expiresAt;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <summary>
    /// Cursor position.
    /// </summary>
    public int Position => _cursor;

    /// <inheritdoc />
    public int TotalRows => Count;

    /// <summary>
    /// Whether the cache is complete and no more rows can be added.
    /// </summary>
    internal bool IsFrozen => _isFrozen;

    /// <summary>
    /// Total cache rows.
    /// </summary>
    public int Count => _cache.Count;

    /// <inheritdoc />
    public Row Current => _currentRow;

    /// <summary>
    /// Rows iterator behind the cache.
    /// </summary>
    internal IRowsIterator RowsIterator => _rowsIterator;

    /// <summary>
    /// Is the current cache cursor position at the last row.
    /// </summary>
    public bool EndOfCache => Position + 1 >= _cache.Count;

    /// <summary>
    /// If cache is expired.
    /// </summary>
    public bool IsExpired => _expiresIn != TimeSpan.Zero && _expiresAt != default && DateTime.UtcNow > _expiresAt;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rowsIterator">Row iterator.</param>
    /// <param name="cacheSize">Max cache size. -1 for no limit.</param>
    /// <param name="expiresIn">Expiration time.</param>
    public CacheRowsIterator(IRowsIterator rowsIterator, int cacheSize = -1, TimeSpan? expiresIn = null)
    {
        _currentRow = new Row(rowsIterator); // Empty.
        _rowsIterator = rowsIterator;
        _cacheSize = cacheSize;
        _expiresIn = expiresIn ?? TimeSpan.Zero;
        _cache = new List<Row>(_cacheSize > 0 ? _cacheSize : 32);
    }

    internal CacheRowsIterator(IRowsIterator rowsIterator, CacheRowsIterator cache) : this(rowsIterator)
    {
        _cache = cache._isFrozen ? cache._cache : new List<Row>(cache._cache);
        _cacheSize = cache._cacheSize;
        _expiresIn = cache._expiresIn;
        _expiresAt = cache._expiresAt;
        _isFrozen = cache._isFrozen;
        if (cache._rowsIterator == rowsIterator)
        {
            _rowsIteratorCursor = cache._rowsIteratorCursor;
        }
    }

    /// <summary>
    /// Get row at the specified position.
    /// </summary>
    /// <param name="position">Row index/position.</param>
    /// <returns>Row instance.</returns>
    public Row GetAt(int position) => _cache[position];

    /// <summary>
    /// Add row manually to the end of the cache. It ignores the max cache limit.
    /// </summary>
    /// <param name="row">Row to add.</param>
    public void AddLast(Row row)
    {
        if (IsFrozen)
        {
            return;
        }
        _cache.Add(new Row(row));
    }

    /// <summary>
    /// Remove row at beginning of cache.
    /// </summary>
    /// <param name="count">How many rows to remove.</param>
    public bool RemoveFirst(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count > _cache.Count || IsFrozen)
        {
            return false;
        }
        _cache.RemoveRange(0, count);
        SetCursor(Math.Max(InitialPosition, _cursor - count));
        return true;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        // If this is the first call, make new expiration date.
        if (_cursor == InitialPosition && _expiresAt == default)
        {
            ResetExpiration();
        }

        // If our position within the cache - return cached data.
        if (_cursor + 1 < _cache.Count)
        {
            SetCursor(_cursor + 1);
            return true;
        }

        if (_isFrozen)
        {
            return false;
        }

        // After finishing the cache items and if it is not frozen, we start getting records from
        // rows iterator.
        if (_cursor != _rowsIteratorCursor)
        {
            await MoveRowsIteratorToCursorPositionAsync(_cursor, cancellationToken);
        }

        var hasData = await _rowsIterator.MoveNextAsync(cancellationToken);
        if (!hasData)
        {
            _isFrozen = _cacheSize == -1 || _cursor < _cache.Count;
            return false;
        }

        _rowsIteratorCursor++;

        // Move next and add to cache.
        _currentRow = _rowsIterator.Current;
        if (_cursor < _cacheSize || _cacheSize == -1)
        {
            _cache.Add(new Row(_currentRow));
        }
        _cursor++;
        return true;
    }

    private async ValueTask MoveRowsIteratorToCursorPositionAsync(int position, CancellationToken cancellationToken)
    {
        if (position < 0)
        {
            return;
        }

        if (_rowsIterator is ICursorRowsIterator cursorRowsIterator)
        {
            if (cursorRowsIterator.Position != position)
            {
                cursorRowsIterator.Seek(position, CursorSeekOrigin.Begin);
            }
            _rowsIteratorCursor = position;
            return;
        }

        if (_rowsIteratorCursor > position)
        {
            await _rowsIterator.ResetAsync(cancellationToken);
            _rowsIteratorCursor = InitialPosition;
        }
        while (_rowsIteratorCursor < position && await _rowsIterator.MoveNextAsync(cancellationToken))
        {
            _rowsIteratorCursor++;
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _rowsIterator.ResetAsync(cancellationToken);
        _rowsIteratorCursor = InitialPosition;
        _isFrozen = false;
        _cache = new List<Row>(_cacheSize > 0 ? _cacheSize : 32);
        _cursor = InitialPosition;
        _currentRow = new Row(_rowsIterator);
        ResetExpiration();
    }

    private void ResetExpiration()
    {
        if (_expiresIn != TimeSpan.Zero)
        {
            _expiresAt = DateTime.UtcNow + _expiresIn;
        }
    }

    /// <summary>
    /// Move cursor position to the beginning of data.
    /// </summary>
    public void SeekCacheCursorToHead()
    {
        SetCursor(InitialPosition);
    }

    /// <summary>
    /// Freezes the cache. In this mode new rows cannot be added.
    /// </summary>
    public void Freeze()
    {
        _isFrozen = true;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        var expireText = _expiresIn == TimeSpan.Zero ? "none" : _expiresAt.ToString("0", CultureInfo.InvariantCulture);
        var text = $"Cache (max={_cacheSize} fill={_cache.Count} expire={expireText} pos={_cursor})";
        stringBuilder.AppendRowsIteratorsWithIndent(text, _rowsIterator);
    }

    /// <inheritdoc />
    public void Seek(int offset, CursorSeekOrigin origin)
    {
        var newPosition = origin switch
        {
            CursorSeekOrigin.Begin => offset,
            CursorSeekOrigin.Current => _cursor + offset,
            CursorSeekOrigin.End => TotalRows - offset - 1,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        ArgumentOutOfRangeException.ThrowIfLessThan(newPosition, InitialPosition, nameof(offset));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(newPosition, _cache.Count, nameof(offset));
        SetCursor(newPosition);
    }

    private void SetCursor(int newPosition)
    {
        _cursor = newPosition;

        if (_cursor == InitialPosition)
        {
            _currentRow = new Row(_rowsIterator);
        }
        else if (_cursor > InitialPosition)
        {
            _currentRow = _cache[_cursor];
        }
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
