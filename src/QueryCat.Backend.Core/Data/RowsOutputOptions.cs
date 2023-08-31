namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Options of rows output.
/// </summary>
public class RowsOutputOptions
{
    /// <summary>
    /// If possible the host application will try to adjust columns lengths.
    /// It can be useful for console output.
    /// </summary>
    public bool RequiresColumnsLengthAdjust { get; init; }
}
