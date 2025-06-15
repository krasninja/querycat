using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator is to perform writing to output on moving next.
/// </summary>
internal sealed class OutputWriteRowsIterator(VaryingOutputRowsIterator rowsIterator)
    : IRowsIterator, IRowsIteratorParent
{
    /// <inheritdoc />
    public Column[] Columns => rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => rowsIterator.Current;

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        while (await rowsIterator.MoveNextAsync(cancellationToken))
        {
            await rowsIterator.CurrentOutput.WriteValuesAsync(rowsIterator.Current.Values, cancellationToken);
        }
        return false;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await rowsIterator.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("WriteToOutput", rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return rowsIterator;
    }
}
