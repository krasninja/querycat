using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Rows input keys.
/// </summary>
public interface IRowsInputKeys : IRowsInput
{
    /// <summary>
    /// Get key columns list.
    /// </summary>
    /// <returns>Keys columns.</returns>
    IReadOnlyList<KeyColumn> GetKeyColumns();

    /// <summary>
    /// Set key column value. Initialize rows input source.
    /// </summary>
    /// <param name="columnIndex">Column index within rows input.</param>
    /// <param name="value">The value.</param>
    /// <param name="operation">Key operation.</param>
    void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation);

    /// <summary>
    /// Unset the key for a specific column index and operation.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="operation">Operation.</param>
    void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation);
}
