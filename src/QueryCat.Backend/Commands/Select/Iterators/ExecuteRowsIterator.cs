using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Commands.Select.Iterators;

internal sealed class ExecuteRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public ExecuteRowsIterator(IRowsIterator rowsIterator)
    {
        _rowsIterator = rowsIterator;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        while (await _rowsIterator.MoveNextAsync(cancellationToken))
        {
        }
        return false;
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return _rowsIterator.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Execute", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
