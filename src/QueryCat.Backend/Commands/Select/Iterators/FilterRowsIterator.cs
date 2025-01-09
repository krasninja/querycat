using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator filters the row data by provided delegate.
/// </summary>
internal sealed class FilterRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IExecutionThread _thread;
    private readonly IRowsIterator _rowsIterator;
    private readonly IFuncUnit _predicate;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public FilterRowsIterator(
        IExecutionThread thread,
        IRowsIterator rowsIterator,
        IFuncUnit predicate)
    {
        _thread = thread;
        _rowsIterator = rowsIterator;
        _predicate = predicate;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        while (await _rowsIterator.MoveNextAsync(cancellationToken))
        {
            if ((await _predicate.InvokeAsync(_thread, cancellationToken)).AsBoolean)
            {
                return true;
            }
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
        stringBuilder.AppendRowsIteratorsWithIndent("Filter", _rowsIterator);
        IndentedStringBuilderUtils.AppendSubQueriesWithIndent(stringBuilder, _predicate);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
