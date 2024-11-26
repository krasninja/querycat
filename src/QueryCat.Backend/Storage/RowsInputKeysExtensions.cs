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
        foreach (var keyColumn in rowsInputKeys.GetKeyColumns())
        {
            if (rowsInputKeys.Columns[keyColumn.ColumnIndex] != column)
            {
                continue;
            }

            if (keyColumn.ContainsOperation(operation)
                && (!orOperation.HasValue || keyColumn.ContainsOperation(orOperation.Value)))
            {
                return keyColumn;
            }

            // Special condition for IN clause.
            if (operation == VariantValue.Operation.In
                && orOperation == null
                && keyColumn.Operation1 == VariantValue.Operation.Equals)
            {
                return keyColumn;
            }
        }

        return null;
    }
}
