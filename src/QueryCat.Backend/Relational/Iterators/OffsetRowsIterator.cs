using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator makes offset in rows reading.
/// </summary>
internal sealed class OffsetRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly long _offset;
    private readonly IRowsIterator _rowsIterator;
    private long _count;
    private bool _isInitialized;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public OffsetRowsIterator(IRowsIterator rowsIterator, long offset)
    {
        ArgumentNullException.ThrowIfNull(rowsIterator);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        _rowsIterator = rowsIterator;
        _offset = offset;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        if (_offset > 0 && _count < _offset)
        {
            while (_offset > _count)
            {
                if (!await _rowsIterator.MoveNextAsync(cancellationToken))
                {
                    return false;
                }
                _count++;
            }
        }

        return await _rowsIterator.MoveNextAsync(cancellationToken);
    }

    private void Initialize()
    {
        if (_rowsIterator is ICursorRowsIterator cursor && cursor.TotalRows > 0)
        {
            var seekPosition = (int)Math.Min(_offset, cursor.TotalRows);
            cursor.Seek(seekPosition, CursorSeekOrigin.Begin);
            _count = _offset; // Mark skip phase as complete.
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _rowsIterator.ResetAsync(cancellationToken);
        _count = 0;
        _isInitialized = false;
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
