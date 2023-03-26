using QueryCat.Backend.Types;

namespace QueryCat.Backend.Abstractions;

/// <summary>
/// Rows input that supports atomic values updates.
/// </summary>
public interface IRowsInputUpdate
{
    /// <summary>
    /// Update the column value at the current position.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="value">New value.</param>
    /// <returns>Error code.</returns>
    ErrorCode UpdateValue(int columnIndex, in VariantValue value);
}
