using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Event arguments on rows input processing.
/// </summary>
public sealed class RowsInputErrorEventArgs : EventArgs
{
    /// <summary>
    /// The row index where error occured.
    /// </summary>
    public long RowIndex { get; }

    /// <summary>
    /// The column index where error occured.
    /// </summary>
    public int ColumnIndex { get; }

    /// <summary>
    /// Error code.
    /// </summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>
    /// Value with error. Optional.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public RowsInputErrorEventArgs(long rowIndex, int columnIndex, ErrorCode errorCode, string? value = null)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        ErrorCode = errorCode;
        Value = value ?? string.Empty;
    }
}
