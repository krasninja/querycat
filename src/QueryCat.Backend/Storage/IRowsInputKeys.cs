using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

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
    /// <param name="columnName">Column name.</param>
    /// <param name="value">The value.</param>
    void SetKeyColumnValue(string columnName, VariantValue value);
}
