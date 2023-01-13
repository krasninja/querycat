using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// Utilities for <see cref="IRowsIterator" />.
/// </summary>
internal static class RowsIteratorUtils
{
    public static bool GetFrameFromIterator(IRowsIterator rowsIterator, out RowsFrame rowsFrame)
    {
        if (rowsIterator is RowsFrameIterator rowsFrameIterator)
        {
            rowsFrame = rowsFrameIterator.RowsFrame;
            return true;
        }
        if (rowsIterator is GroupRowsIterator groupRowsIterator)
        {
            rowsFrame = groupRowsIterator.RowsFrame;
            return true;
        }
        if (rowsIterator is OrderRowsIterator orderRowsIterator)
        {
            rowsFrame = orderRowsIterator.RowsFrame;
            return true;
        }
        rowsFrame = new RowsFrame(rowsIterator.Columns);
        return false;
    }
}
