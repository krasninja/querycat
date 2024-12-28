using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator writes new row into <see cref="RowsFrame" />.
/// </summary>
internal sealed class WriteRowsFrameIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly RowsFrame _rowsFrame;
    private readonly IRowsIterator _rowsIterator;

    /// <inheritdoc />
    public Column[] Columns => _rowsFrame.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public WriteRowsFrameIterator(RowsFrame rowsFrame, IRowsIterator rowsIterator)
    {
        _rowsFrame = rowsFrame;
        _rowsIterator = rowsIterator;

        if (!_rowsFrame.IsSchemaEqual(rowsIterator))
        {
            throw new InvalidOperationException(Resources.Errors.SchemasNotEqual);
        }
    }

    /// <summary>
    /// Write all data from rows iterator to frame.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of added rows.</returns>
    public async ValueTask<int> WriteAllAsync(CancellationToken cancellationToken = default)
    {
        var total = 0;
        while (await MoveNextAsync(cancellationToken))
        {
            total++;
        }
        return total;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        var hasData = await _rowsIterator.MoveNextAsync(cancellationToken);
        if (hasData)
        {
            _rowsFrame.AddRow(_rowsIterator.Current);
        }
        return hasData;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Write To Frame");
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
