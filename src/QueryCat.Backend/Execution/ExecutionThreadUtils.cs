using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Utilities for <see cref="ExecutionThread" />.
/// </summary>
public static class ExecutionThreadUtils
{
    /// <summary>
    /// Convert variant value to enumerable of rows. It analyzes the internal
    /// content of value and generates the correspond output.
    /// </summary>
    /// <param name="variantValue">Variant value.</param>
    /// <returns>Enumerable of rows.</returns>
    public static IRowsIterator ConvertToIterator(VariantValue variantValue)
    {
        var type = variantValue.GetInternalType();
        if (type == DataType.Null)
        {
            return new EmptyIterator();
        }

        if (type == DataType.Object)
        {
            var obj = variantValue.AsObject;
            // IRowsIterator.
            if (obj is IRowsIterator rowsIterator)
            {
                return rowsIterator;
            }
            // IRowsInput.
            else if (obj is IRowsInput rowsInput)
            {
                return new RowsInputIterator(rowsInput, autoOpen: true);
            }
            // IRowsOutput.
            else if (obj is IRowsOutput)
            {
                return new SingleValueRowsIterator();
            }
        }

        return new SingleValueRowsIterator(variantValue);
    }
}
