using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator adds the statistic processing.
/// </summary>
public sealed class StatisticRowsIterator : IRowsIterator, IRowsIteratorParent
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

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(StatisticRowsIterator));

    public StatisticRowsIterator(IRowsIterator rowsIterator, ExecutionStatistic statistic)
    {
        _rowsIterator = rowsIterator;
        _statistic = statistic;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (MaxErrorsCount > -1 && _statistic.ErrorsCount >= MaxErrorsCount)
        {
            _logger.LogCritical(
                "Maximum number of errors reached! Maximum {MaxErrorsCount}, current {ErrorsCount}.",
                MaxErrorsCount,
                _statistic.ErrorsCount);
            return false;
        }

        var result = await _rowsIterator.MoveNextAsync(cancellationToken);
        if (result)
        {
            _statistic.ProcessedCount++;
        }
        return result;
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return _rowsIterator.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Stat (count={_statistic.ProcessedCount})", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
