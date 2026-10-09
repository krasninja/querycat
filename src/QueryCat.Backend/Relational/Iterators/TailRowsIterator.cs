using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator returns the specific number of rows from the source end.
/// </summary>
internal sealed class TailRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private readonly int _tailCount;
    private bool _isInitialized;
    private IRowsIterator _currentRowsIterator;
    private readonly Queue<Row> _tailBuffer;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current => _currentRowsIterator.Current;

    public TailRowsIterator(IRowsIterator rowsIterator, int tailCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tailCount);

        _rowsIterator = rowsIterator;
        _currentRowsIterator = rowsIterator;
        _tailCount = tailCount;
        _tailBuffer = new Queue<Row>(capacity: Math.Min(tailCount, 32));
        Columns = _rowsIterator.Columns;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(cancellationToken);
        }

        return await _currentRowsIterator.MoveNextAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _tailBuffer.Clear();
        _isInitialized = false;
        await _rowsIterator.ResetAsync(cancellationToken);
        _currentRowsIterator = _rowsIterator;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Tail (count={_tailCount})", _rowsIterator);
    }

    private sealed class QueueRowsIterator(Column[] columns, Queue<Row> queue) : IRowsIterator
    {
        /// <inheritdoc />
        public Column[] Columns => columns;

        /// <inheritdoc />
        public Row Current { get; private set; } = new();

        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            if (queue.Count == 0)
            {
                return ValueTask.FromResult(false);
            }
            Current = queue.Dequeue();
            return ValueTask.FromResult(true);
        }

        /// <inheritdoc />
        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Explain(IndentedStringBuilder stringBuilder)
        {
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_tailCount == 0)
        {
            _isInitialized = true;
            return;
        }

        if (_rowsIterator is ICursorRowsIterator cursorRowsIterator && cursorRowsIterator.TotalRows > 0)
        {
            var seekPosition = Math.Max(0, cursorRowsIterator.TotalRows - _tailCount);
            cursorRowsIterator.Seek(seekPosition, CursorSeekOrigin.Begin);
            _currentRowsIterator = _rowsIterator;
        }
        else
        {
            while (await _rowsIterator.MoveNextAsync(cancellationToken))
            {
                _tailBuffer.Enqueue(new Row(_rowsIterator.Current));
                if (_tailBuffer.Count > _tailCount)
                {
                    _tailBuffer.Dequeue();
                }
            }

            _currentRowsIterator = new QueueRowsIterator(Columns, _tailBuffer);
        }

        _isInitialized = true;
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
