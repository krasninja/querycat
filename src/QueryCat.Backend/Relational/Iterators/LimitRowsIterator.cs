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
        ArgumentOutOfRangeException.ThrowIfNegative(limit, nameof(limit));

        _rowsIterator = rowsIterator;
        _limit = limit;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (_count >= _limit)
        {
            return false;
        }
        if (await _rowsIterator.MoveNextAsync(cancellationToken))
        {
            _count++;
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _rowsIterator.ResetAsync(cancellationToken);
        _count = 0;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Limit (rows={_limit})", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
