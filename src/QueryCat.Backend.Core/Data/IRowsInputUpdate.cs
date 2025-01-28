using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Rows input that supports atomic values updates.
/// </summary>
public interface IRowsInputUpdate : IRowsInput
{
    /// <summary>
    /// Update the column value at the current position. The actual update may occur after ReadNext call.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="value">New value.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>Error code.</returns>
    ValueTask<ErrorCode> UpdateValueAsync(int columnIndex, VariantValue value, CancellationToken cancellationToken = default);
}
