using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Rows input that supports atomic values updates.
/// </summary>
public interface IRowsInputUpdate : IRowsInput
{
    /// <summary>
    /// Update the column value at the current position.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="value">New value.</param>
    /// <returns>Error code.</returns>
    ErrorCode UpdateValue(int columnIndex, in VariantValue value);
}
