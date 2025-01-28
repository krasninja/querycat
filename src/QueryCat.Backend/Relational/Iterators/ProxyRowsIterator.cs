using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator just proxies all the requests to inner iterator which can be replaced.
/// </summary>
internal sealed class ProxyRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private IRowsIterator _currentIterator;

    /// <inheritdoc />
    public Column[] Columns => _currentIterator.Columns;

    /// <inheritdoc />
    public Row Current => _currentIterator.Current;

    public IRowsIterator CurrentIterator => _currentIterator;

    public ProxyRowsIterator(IRowsIterator currentIterator)
    {
        _currentIterator = currentIterator;
    }

    public ProxyRowsIterator(IRowsSchema schema)
    {
        _currentIterator = new EmptyIterator(schema);
    }

    /// <summary>
    /// Set another iterator.
    /// </summary>
    /// <param name="rowsIterator">New iterator.</param>
    public void Set(IRowsIterator rowsIterator)
    {
        _currentIterator = rowsIterator;
    }

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        => _currentIterator.MoveNextAsync(cancellationToken);

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return _currentIterator.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Proxy", _currentIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _currentIterator;
    }
}
