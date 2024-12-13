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
    public static void Write(this IRowsOutput output, IRowsIterator iterator, bool adjustColumnsLengths = false)
    {
        output.QueryContext = new RowsOutputQueryContext(iterator.Columns);
        output.Open();
        try
        {
            if (adjustColumnsLengths)
            {
                iterator = new AdjustColumnsLengthsIterator(iterator);
            }
            while (iterator.MoveNext())
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
    public static void Write(this IRowsOutput output, IRowsInput input, bool adjustColumnsLengths = false)
    {
        Write(output, new RowsInputIterator(input), adjustColumnsLengths);
    }
}
