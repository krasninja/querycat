using System.Text;
using QueryCat.Backend.Utils;

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
    public readonly struct RowErrorInfo
    {
        public long RowIndex { get; }

        public int ColumnIndex { get; }

        public ErrorCode ErrorCode { get; }

        public string Value { get; }

        public RowErrorInfo(long rowIndex, int columnIndex, ErrorCode errorCode, string? value = null)
        {
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            ErrorCode = errorCode;
            Value = value ?? string.Empty;
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
    public long ErrorsCount { get; internal set; }

    /// <summary>
    /// Does it have errors.
    /// </summary>
    public bool HasErrors => ErrorsCount > 0;

    /// <summary>
    /// Rows indexes with error details.
    /// </summary>
    public IReadOnlyList<RowErrorInfo> ErrorRows => _errorRows;

    /// <summary>
    /// Save rows with errors and show them in statistic.
    /// </summary>
    public bool CountErrorRows { get; set; }

    /// <summary>
    /// Add error to the query statistic.
    /// </summary>
    /// <param name="code">Error code.</param>
    public void IncrementErrorsCount(ErrorCode code)
    {
        if (code == ErrorCode.OK)
        {
            return;
        }
        ErrorsCount++;
        _statistic.AddOrUpdate(code, _ => 1, (_, value) => ++value);
    }

    /// <summary>
    /// Add error to the query statistic.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="row">Row index.</param>
    /// <param name="column">Column index.</param>
    /// <param name="value">Error value.</param>
    public void IncrementErrorsCount(ErrorCode code, long row, int column, string? value = null)
    {
        IncrementErrorsCount(code);
        if (CountErrorRows)
        {
            _errorRows.Add(new RowErrorInfo(row, column, code, value));
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

    /// <inheritdoc />
    public override string ToString()
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

        if (CountErrorRows && _errorRows.Any())
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
}
