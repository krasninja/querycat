using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator executes custom action before and/or after the next row fetch.
/// </summary>
internal sealed class ActionRowsIterator : IRowsIterator, IRowsIteratorParent
{
    public string Message { get; set; }

    private readonly IRowsIterator _rowsIterator;

    public Func<IRowsIterator, CancellationToken, ValueTask>? BeforeMoveNext { get; set; }

    public Func<IRowsIterator, CancellationToken, ValueTask>? AfterMoveNext { get; set; }

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public ActionRowsIterator(IRowsIterator rowsIterator, string message)
    {
        Message = message;
        _rowsIterator = rowsIterator;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (BeforeMoveNext != null)
        {
            await BeforeMoveNext.Invoke(_rowsIterator, cancellationToken);
        }
        var result = await _rowsIterator.MoveNextAsync(cancellationToken);
        if (result)
        {
            if (AfterMoveNext != null)
            {
                await AfterMoveNext.Invoke(_rowsIterator, cancellationToken);
            }
        }
        return result;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent(
            $"Action (msg='{Message}' before={BeforeMoveNext != null} after={AfterMoveNext != null})", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
