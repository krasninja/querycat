namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Execution statistic.
/// </summary>
public abstract class ExecutionStatistic
{
    /// <summary>
    /// Detailed row error info.
    /// </summary>
    public readonly struct RowErrorInfo(
        ErrorCode errorCode,
        long rowIndex,
        int columnIndex,
        string? value = null)
    {
        /// <summary>
        /// Zero-based row index.
        /// </summary>
        public long RowIndex { get; } = rowIndex;

        /// <summary>
        /// Zero-based column index.
        /// </summary>
        public int ColumnIndex { get; } = columnIndex;

        /// <summary>
        /// Error code.
        /// </summary>
        public ErrorCode ErrorCode { get; } = errorCode;

        /// <summary>
        /// Wrong value.
        /// </summary>
        public string Value { get; } = value ?? string.Empty;

        public RowErrorInfo(ErrorCode errorCode) : this(errorCode, -1, -1)
        {
        }
    }

    /// <summary>
    /// Query processing execution time.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Total number of processed rows.
    /// </summary>
    public long ProcessedCount { get; set; }

    /// <summary>
    /// Errors count.
    /// </summary>
    public long ErrorsCount { get; protected set; }

    /// <summary>
    /// Does it have errors.
    /// </summary>
    public bool HasErrors => ErrorsCount > 0;

    /// <summary>
    /// Add error to the query statistic.
    /// </summary>
    /// <param name="info">Error info.</param>
    public abstract void AddError(in RowErrorInfo info);

    /// <summary>
    /// Reset statistic.
    /// </summary>
    public virtual void Clear()
    {
        ExecutionTime = TimeSpan.Zero;
        ProcessedCount = 0;
        ErrorsCount = 0;
    }

    /// <summary>
    /// Dump statistic as string.
    /// </summary>
    public abstract string Dump();
}
