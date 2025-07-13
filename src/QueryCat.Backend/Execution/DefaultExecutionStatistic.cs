using System.Text;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The class contains execution statistic (execution time, errors, etc).
/// </summary>
public sealed class DefaultExecutionStatistic : ExecutionStatistic
{
    /// <summary>
    /// Errors total count by codes.
    /// </summary>
    private readonly Dictionary<ErrorCode, long> _statistic = new();

    private readonly List<RowErrorInfo> _errorRows = new();

    /// <inheritdoc />
    public override IReadOnlyList<RowErrorInfo> Errors => _errorRows;

    /// <inheritdoc />
    public override void AddError(in RowErrorInfo info)
    {
        if (info.ErrorCode == ErrorCode.OK)
        {
            return;
        }
        ErrorsCount++;
        _statistic.AddOrUpdate(info.ErrorCode, _ => 1, (_, value) => ++value);
        if (info.RowIndex > -1)
        {
            _errorRows.Add(info);
        }
    }

    /// <inheritdoc />
    public override void Clear()
    {
        base.Clear();
        _statistic.Clear();
    }

    /// <inheritdoc />
    public override string Dump()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Execution time: " + ExecutionTime);
        sb.AppendLine("Rows processed: " + ProcessedCount);

        if (HasErrors)
        {
            sb.AppendLine("Total errors: " + ErrorsCount);
            sb.AppendLine("Errors:");
            foreach (var item in _statistic)
            {
                sb.AppendLine($"- {item.Key}: {item.Value}");
            }
        }

        if (_errorRows.Any())
        {
            sb.AppendLine(new string('-', 5));
            sb.AppendLine("Rows with error(-s):");
            foreach (var errorInfo in _errorRows)
            {
                var message = string.IsNullOrEmpty(errorInfo.Value)
                    ? $"{errorInfo.ErrorCode}"
                    : $"{errorInfo.ErrorCode} with {errorInfo.Value}";
                sb.AppendLine($"row {errorInfo.RowIndex}, column {errorInfo.ColumnIndex}: {message}.");
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public override string ToString() => $"Execution time: {ExecutionTime}, rows processed: {ProcessedCount}";
}
