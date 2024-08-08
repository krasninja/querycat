namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Rows input that supports atomic rows remove.
/// </summary>
public interface IRowsInputDelete : IRowsInput
{
    /// <summary>
    /// Delete the current row. The ReadNext should be called after that to fetch new row.
    /// The ReadValue can return Deleted error code.
    /// </summary>
    /// <returns>Error code.</returns>
    ErrorCode DeleteCurrent();
}
