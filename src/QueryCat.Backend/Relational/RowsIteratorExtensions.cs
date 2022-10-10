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
}
