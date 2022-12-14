using Serilog;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator adds the statistic processing.
/// </summary>
public sealed class StatisticRowsIterator : IRowsIterator
{
    private readonly IRowsIterator _rowsIterator;
    private readonly ExecutionStatistic _statistic;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    /// <summary>
    /// Max count of errors before abort.
    /// </summary>
    public int MaxErrorsCount { get; set; } = -1;

    public StatisticRowsIterator(IRowsIterator rowsIterator, ExecutionStatistic statistic)
    {
        _rowsIterator = rowsIterator;
        _statistic = statistic;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (MaxErrorsCount > -1 && _statistic.ErrorsCount >= MaxErrorsCount)
        {
            Log.Logger.Fatal(
                "Maximum number of errors reached! Maximum {MaxErrorsCount}, current {ErrorsCount}.",
                MaxErrorsCount,
                _statistic.ErrorsCount);
            return false;
        }

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
        stringBuilder.AppendRowsIteratorsWithIndent($"Stat (count={_statistic.ProcessedCount})", _rowsIterator);
    }
}
