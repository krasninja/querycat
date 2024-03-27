using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Extensions for <see cref="IRowsInputKeys" />.
/// </summary>
public static class RowsInputKeysExtensions
{
    /// <summary>
    /// Find key column.
    /// </summary>
    /// <param name="rowsInputKeys">Rows input with keys.</param>
    /// <param name="column">Column.</param>
    /// <param name="operation">Operation.</param>
    /// <param name="orOperation">Alternative operation.</param>
    /// <returns>Key column or null if not found.</returns>
    public static KeyColumn? FindKeyColumn(
        this IRowsInputKeys rowsInputKeys,
        Column column,
        VariantValue.Operation operation,
        VariantValue.Operation? orOperation = null)
    {
        return rowsInputKeys.GetKeyColumns()
            .FirstOrDefault(k =>
                rowsInputKeys.Columns[k.ColumnIndex] == column
                && k.ContainsOperation(operation)
                && (!orOperation.HasValue || k.ContainsOperation(orOperation.Value)));
    }
}
