using QueryCat.Backend.Execution;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator just count the number of processed rows. It fills the execution statistic.
/// </summary>
public sealed class CountRowsIterator : IRowsIterator
{
    private readonly IRowsIterator _rowsIterator;
    private readonly ExecutionStatistic _statistic;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public CountRowsIterator(IRowsIterator rowsIterator, ExecutionStatistic statistic)
    {
        _rowsIterator = rowsIterator;
        _statistic = statistic;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        var result = _rowsIterator.MoveNext();
        if (result)
        {
            _statistic.ProcessedCount++;
        }
        return result;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
        _statistic.Clear();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Count (count={_statistic.ProcessedCount})", _rowsIterator);
    }
}
