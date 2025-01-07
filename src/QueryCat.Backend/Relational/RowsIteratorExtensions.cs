using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Extensions for <see cref="IRowsIterator" />.
/// </summary>
public static class RowsIteratorExtensions
{
    /// <summary>
    /// Create rows frame from iterator.
    /// </summary>
    /// <param name="iterator">Iterator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="RowsFrame" />.</returns>
    public static Task<RowsFrame> ToFrameAsync(this IRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        var frame = new RowsFrame(iterator.Columns);
        return ToFrameAsync(iterator, frame, cancellationToken);
    }

    /// <summary>
    /// Create rows frame from iterator.
    /// </summary>
    /// <param name="iterator">Iterator.</param>
    /// <param name="count">Max number of rows to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="RowsFrame" />.</returns>
    public static Task<RowsFrame> ToFrameAsync(this IRowsIterator iterator, int count, CancellationToken cancellationToken = default)
    {
        var frame = new RowsFrame(iterator.Columns);
        return ToFrameAsync(iterator, frame, count, cancellationToken);
    }

    /// <summary>
    /// Fills rows frame from iterator.
    /// </summary>
    /// <param name="iterator">Iterator.</param>
    /// <param name="frame">Rows frame to fill.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="RowsFrame" />.</returns>
    public static async Task<RowsFrame> ToFrameAsync(this IRowsIterator iterator, RowsFrame frame,
        CancellationToken cancellationToken = default)
    {
        while (await iterator.MoveNextAsync(cancellationToken))
        {
            frame.AddRow(iterator.Current);
        }
        return frame;
    }

    /// <summary>
    /// Fills rows frame from iterator.
    /// </summary>
    /// <param name="iterator">Iterator.</param>
    /// <param name="frame">Rows frame to fill.</param>
    /// <param name="count">Max number of rows to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="RowsFrame" />.</returns>
    public static async Task<RowsFrame> ToFrameAsync(
        this IRowsIterator iterator,
        RowsFrame frame,
        int count,
        CancellationToken cancellationToken = default)
    {
        while (frame.TotalRows < count && await iterator.MoveNextAsync(cancellationToken))
        {
            frame.AddRow(iterator.Current);
        }
        return frame;
    }
}
