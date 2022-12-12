using QueryCat.Backend.Abstractions;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Extensions for <see cref="IRowsInput" />.
/// </summary>
public static class RowsInputExtensions
{
    /// <summary>
    /// Create iterator for rows input.
    /// </summary>
    /// <param name="input">Rows input.</param>
    /// <param name="autoFetch">Fetch all columns values for iterator.</param>
    /// <returns>Instance of <see cref="RowsInputIterator" />.</returns>
    public static RowsInputIterator AsIterable(this IRowsInput input, bool autoFetch = false)
        => new(input, autoFetch);
}
