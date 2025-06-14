using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// Pre-reads the first row.
/// </summary>
internal sealed class PrefetchRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private bool _firstRead;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    private PrefetchRowsIterator(IRowsIterator rowsIterator)
    {
        _rowsIterator = rowsIterator;
    }

    public static async Task<PrefetchRowsIterator> CreateAsync(IRowsIterator iterator, CancellationToken cancellationToken)
    {
        var prefetchRowsIterator = new PrefetchRowsIterator(iterator);
        var hasData = await prefetchRowsIterator.MoveNextAsync(cancellationToken);
        if (hasData)
        {
            prefetchRowsIterator._firstRead = true;
        }
        return prefetchRowsIterator;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (_firstRead)
        {
            _firstRead = false;
            return true;
        }
        return await _rowsIterator.MoveNextAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) => _rowsIterator.ResetAsync(cancellationToken);

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Prefetch", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
