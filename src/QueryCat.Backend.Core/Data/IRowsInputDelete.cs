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
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>Error code.</returns>
    ValueTask<ErrorCode> DeleteAsync(CancellationToken cancellationToken = default);
}
