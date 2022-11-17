using QueryCat.Backend.Formatters;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Extensions for <see cref="IRowsOutput" />.
/// </summary>
public static class RowsOutputExtensions
{
    /// <summary>
    /// Write variant value to rows output.
    /// </summary>
    /// <param name="rowsOutput">Instance of <see cref="IRowsOutput" />.</param>
    /// <param name="variantValue">Value.</param>
    public static void Write(this IRowsOutput rowsOutput, VariantValue variantValue)
    {
        var type = variantValue.GetInternalType();
        if (type == DataType.Object)
        {
            var obj = variantValue.AsObject;
            if (obj is IRowsIterator rowsIterator)
            {
                rowsOutput.Write(rowsIterator);
            }
            else if (obj is IRowsInput rowsInput)
            {
                rowsInput.Open();
                rowsOutput.Write(new RowsInputIterator(rowsInput));
                rowsInput.Close();
            }
            return;
        }

        if (!variantValue.IsNull)
        {
            var singleValueIterator = new SingleValueRowsIterator(variantValue);
            rowsOutput.Write(singleValueIterator);
        }
    }

    /// <summary>
    /// Write rows iterator to output.
    /// </summary>
    /// <param name="rowsOutput">Instance of <see cref="IRowsOutput" />.</param>
    /// <param name="rowsIterator">Instance of <see cref="IRowsIterator" />.</param>
    public static void Write(this IRowsOutput rowsOutput, IRowsIterator rowsIterator)
    {
        // For plain output let's adjust columns width first.
        if (rowsOutput is TextTableOutput || rowsOutput is PagingOutput)
        {
            rowsIterator = new AdjustColumnsLengthsIterator(rowsIterator);
        }

        var isOpened = false;
        var queryContext = new RowsOutputQueryContext(rowsIterator.Columns);
        while (rowsIterator.MoveNext())
        {
            if (!isOpened)
            {
                rowsOutput.Open();
                rowsOutput.SetContext(queryContext);
                isOpened = true;
            }
            rowsOutput.Write(rowsIterator.Current);
        }
        if (isOpened)
        {
            rowsOutput.Close();
        }
    }
}
