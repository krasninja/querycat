using System.Text;
using QueryCat.Backend.Core;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The class contains execution statistic (execution time, errors, etc).
/// </summary>
public sealed class ExecutionStatistic
{
    /// <summary>
    /// Errors total count by codes.
    /// </summary>
    private readonly Dictionary<ErrorCode, long> _statistic = new();

    /// <summary>
    /// Detailed row error info.
    /// </summary>
    public readonly struct RowErrorInfo(
        ErrorCode errorCode,
        long rowIndex,
        int columnIndex,
        string? value = null)
    {
        public long RowIndex { get; } = rowIndex;

        public int ColumnIndex { get; } = columnIndex;

        public ErrorCode ErrorCode { get; } = errorCode;

        public string Value { get; } = value ?? string.Empty;

        public RowErrorInfo(ErrorCode errorCode) : this(errorCode, -1, -1)
        {
        }
    }

    private readonly List<RowErrorInfo> _errorRows = new();

    /// <summary>
    /// Query processing execution time.
    /// </summary>
    public TimeSpan ExecutionTime { get; internal set; }

    /// <summary>
    /// Total number of processed rows.
    /// </summary>
    public long ProcessedCount { get; internal set; }

    /// <summary>
    /// Errors count.
    /// </summary>
    public long ErrorsCount { get; private set; }

    /// <summary>
    /// Does it have errors.
    /// </summary>
    public bool HasErrors => ErrorsCount > 0;

    /// <summary>
    /// Rows indexes with error details.
    /// </summary>
    public IReadOnlyList<RowErrorInfo> ErrorRows => _errorRows;

    /// <summary>
    /// Add error to the query statistic.
    /// </summary>
    /// <param name="info">Errors info.</param>
    public void AddError(in RowErrorInfo info)
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

    /// <summary>
    /// Clear.
    /// </summary>
    public void Clear()
    {
        ExecutionTime = TimeSpan.Zero;
        ProcessedCount = 0;
        ErrorsCount = 0;
        _statistic.Clear();
    }

    /// <summary>
    /// Dump statistic as string.
    /// </summary>
    /// <returns>Statistic info.</returns>
    public string Dump(bool detailed)
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

        if (detailed && _errorRows.Any())
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
