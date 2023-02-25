using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Extensions for <see cref="IRowsOutput" />.
/// </summary>
public static class RowsOutputExtensions
{
    /// <summary>
    /// Write rows iterator to output.
    /// </summary>
    /// <param name="rowsOutput">Instance of <see cref="IRowsOutput" />.</param>
    /// <param name="rowsIterator">Instance of <see cref="IRowsIterator" />.</param>
    /// <param name="executionThread">Execution thread.</param>
    /// <param name="cancellationToken">Token to notify about query cancellation.</param>
    public static void Write(
        this IRowsOutput rowsOutput,
        IRowsIterator rowsIterator,
        IExecutionThread executionThread,
        CancellationToken cancellationToken = default)
    {
        // For plain output let's adjust columns width first.
        if (rowsOutput is TextTableOutput || rowsOutput is PagingOutput)
        {
            rowsIterator = new AdjustColumnsLengthsIterator(rowsIterator);
        }

        var isOpened = false;
        var queryContext = new RowsOutputQueryContext(rowsIterator.Columns, executionThread);
        while (rowsIterator.MoveNext())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
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
