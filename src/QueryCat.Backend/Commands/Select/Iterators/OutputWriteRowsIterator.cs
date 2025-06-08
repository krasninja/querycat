using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator is to perform writing to output on moving next.
/// </summary>
internal sealed class OutputWriteRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private readonly IRowsOutput _rowsOutput;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public OutputWriteRowsIterator(IRowsIterator rowsIterator, IRowsOutput rowsOutput)
    {
        _rowsIterator = rowsIterator;
        _rowsOutput = rowsOutput;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        while (await _rowsIterator.MoveNextAsync(cancellationToken))
        {
            await _rowsOutput.WriteValuesAsync(_rowsIterator.Current.Values, cancellationToken);
        }
        return false;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _rowsIterator.ResetAsync(cancellationToken);
        await _rowsOutput.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("WriteToOutput", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
