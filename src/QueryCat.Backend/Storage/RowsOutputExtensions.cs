using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Execution;
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
        ExecutionThread executionThread,
        CancellationToken cancellationToken = default)
    {
        // For plain output let's adjust columns width first.
        if (rowsOutput.Options.RequiresColumnsLengthAdjust && executionThread.Options.AnalyzeRowsCount > 0)
        {
            rowsIterator = new AdjustColumnsLengthsIterator(rowsIterator, executionThread.Options.AnalyzeRowsCount);
        }

        // Write the main data.
        var isOpened = false;
        StartWriterLoop();

        // Append grow data.
        if (executionThread.Options.FollowTimeoutMs > 0)
        {
            while (true)
            {
                Thread.Sleep(executionThread.Options.FollowTimeoutMs);
                StartWriterLoop();
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        if (isOpened)
        {
            rowsOutput.Close();
        }

        void StartWriterLoop()
        {
            while (rowsIterator.MoveNext())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                if (!isOpened)
                {
                    rowsOutput.Open();
                    rowsOutput.QueryContext = new RowsOutputQueryContext(rowsIterator.Columns);
                    isOpened = true;
                }
                rowsOutput.WriteValues(rowsIterator.Current.Values);
            }
        }
    }
}
