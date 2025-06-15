using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Extensions for <see cref="IRowsInput" />.
/// </summary>
public static class RowsInputExtensions
{
    /// <summary>
    /// Create iterator for rows input.
    /// </summary>
    /// <param name="input">Rows input.</param>
    /// <param name="autoFetch">Fetch all columns values for iterator.</param>
    /// <returns>Instance of <see cref="RowsInputIterator" />.</returns>
    public static RowsInputIterator AsIterable(this IRowsInput input, bool autoFetch = false)
        => new(input, autoFetch);

    /// <summary>
    /// Find key column.
    /// </summary>
    /// <param name="rowsInput">Rows input with keys.</param>
    /// <param name="column">Column.</param>
    /// <param name="operation">Operation.</param>
    /// <param name="orOperation">Alternative operation.</param>
    /// <returns>Key column or null if not found.</returns>
    public static KeyColumn? FindKeyColumn(
        this IRowsInput rowsInput,
        Column column,
        VariantValue.Operation operation,
        VariantValue.Operation? orOperation = null)
    {
        foreach (var keyColumn in rowsInput.GetKeyColumns())
        {
            if (rowsInput.Columns[keyColumn.ColumnIndex] != column)
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
