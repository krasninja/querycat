namespace QueryCat.Backend.Utils;

/// <summary>
/// Extensions to <see cref="IAsyncEnumerable{T}" />.
/// </summary>
internal static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Converts async enumerable into list.
    /// </summary>
    /// <param name="items">Async enumerable.</param>
    /// <param name="cancellationToken">Cancellation token to monitor request cancellation.</param>
    /// <typeparam name="T">Enumerable type.</typeparam>
    /// <returns>List.</returns>
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            results.Add(item);
        }
        return results;
    }
}
