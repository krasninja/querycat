using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

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
        var type = variantValue.Type;
        if (type == DataType.Null)
        {
            return EmptyIterator.Instance;
        }

        if (type == DataType.Object || type == DataType.Dynamic)
        {
            var obj = variantValue.AsObjectUnsafe;
            // IRowsIterator.
            if (obj is IRowsIterator rowsIterator)
            {
                return rowsIterator;
            }
            // IRowsInput.
            if (obj is IRowsInput rowsInput)
            {
                return new RowsInputIterator(rowsInput, autoOpen: true);
            }
            // IRowsOutput.
            if (obj is IRowsOutput)
            {
                return new SingleValueRowsIterator();
            }
        }

        return new SingleValueRowsIterator(variantValue);
    }
}
