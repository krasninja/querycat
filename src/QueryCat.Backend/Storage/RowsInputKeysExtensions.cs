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
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Operation.</param>
    /// <param name="orOperation">Alternative operation.</param>
    /// <returns>Key column or null if not found.</returns>
    public static KeyColumn? FindKeyColumn(
        this IRowsInputKeys rowsInputKeys,
        string columnName,
        VariantValue.Operation operation,
        VariantValue.Operation? orOperation = null)
    {
        return rowsInputKeys.GetKeyColumns()
            .FirstOrDefault(k => Column.NameEquals(k.ColumnName, columnName)
                && k.ContainsOperation(operation)
                && (!orOperation.HasValue || k.ContainsOperation(orOperation.Value)));
    }
}
