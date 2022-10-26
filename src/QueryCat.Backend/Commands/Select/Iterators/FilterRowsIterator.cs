using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator filters the row data by provided delegate.
/// </summary>
internal sealed class FilterRowsIterator : IRowsIterator
{
    private readonly IRowsIterator _rowsIterator;
    private readonly FuncUnit _predicate;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public FilterRowsIterator(
        IRowsIterator rowsIterator,
        FuncUnit predicate)
    {
        _rowsIterator = rowsIterator;
        _predicate = predicate;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        while (_rowsIterator.MoveNext())
        {
            if (_predicate.Invoke().AsBoolean)
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Filter", _rowsIterator)
            .AppendSubQueriesWithIndent(_predicate);
    }
}
