using QueryCat.Backend.Abstractions;

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
    /// <returns>Instance of <see cref="RowsFrame" />.</returns>
    public static RowsFrame ToFrame(this IRowsIterator iterator)
    {
        var frame = new RowsFrame(iterator.Columns);
        return ToFrame(iterator, frame);
    }

    /// <summary>
    /// Create rows frame from iterator.
    /// </summary>
    /// <param name="iterator">Iterator.</param>
    /// <param name="count">Max number of rows to read.</param>
    /// <returns>Instance of <see cref="RowsFrame" />.</returns>
    public static RowsFrame ToFrame(this IRowsIterator iterator, int count)
    {
        var frame = new RowsFrame(iterator.Columns);
        return ToFrame(iterator, frame, count);
    }

    /// <summary>
    /// Fills rows frame from iterator.
    /// </summary>
    /// <param name="iterator">Iterator.</param>
    /// <param name="frame">Rows frame to fill.</param>
    /// <returns>Instance of <see cref="RowsFrame" />.</returns>
    public static RowsFrame ToFrame(this IRowsIterator iterator, RowsFrame frame)
    {
        while (iterator.MoveNext())
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
    /// <returns>Instance of <see cref="RowsFrame" />.</returns>
    public static RowsFrame ToFrame(this IRowsIterator iterator, RowsFrame frame, int count)
    {
        while (frame.TotalRows < count && iterator.MoveNext())
        {
            frame.AddRow(iterator.Current);
        }
        return frame;
    }
}
