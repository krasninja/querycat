using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Extensions for <see cref="IRowsOutput" />.
/// </summary>
public static class RowsOutputExtensions
{
    /// <summary>
    /// Write rows iterator into output.
    /// </summary>
    /// <param name="output">Rows output.</param>
    /// <param name="iterator">Rows iterator.</param>
    /// <param name="adjustColumnsLengths">Should update columns widths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async ValueTask WriteAsync(this IRowsOutput output, IRowsIterator iterator, bool adjustColumnsLengths = false,
        CancellationToken cancellationToken = default)
    {
        output.QueryContext = new RowsOutputQueryContext(iterator.Columns);
        output.Open();
        try
        {
            if (adjustColumnsLengths)
            {
                iterator = new AdjustColumnsLengthsIterator(iterator);
            }
            while (await iterator.MoveNextAsync(cancellationToken))
            {
                output.WriteValues(iterator.Current.Values);
            }
        }
        finally
        {
            output.Close();
        }
    }

    /// <summary>
    /// Write rows iterator into output.
    /// </summary>
    /// <param name="output">Rows output.</param>
    /// <param name="input">Rows input.</param>
    /// <param name="adjustColumnsLengths">Should update columns widths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static ValueTask WriteAsync(this IRowsOutput output, IRowsInput input, bool adjustColumnsLengths = false,
        CancellationToken cancellationToken = default)
    {
        return WriteAsync(output, new RowsInputIterator(input), adjustColumnsLengths, cancellationToken: cancellationToken);
    }
}
