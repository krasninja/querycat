using QueryCat.Backend.Logging;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The class contains various helper method to simplifies remote source
/// iteration (offset, limit, fetch).
/// </summary>
/// <typeparam name="TClass">Source class type.</typeparam>
public class ClassEnumerableInputHelper<TClass> where TClass : class
{
    private readonly ClassEnumerableInput<TClass> _enumerableInput;

    /// <summary>
    /// Default limit to fetch.
    /// </summary>
    public int Limit { get; set; } = 50;

    /// <summary>
    /// Start page index. Zero by default.
    /// </summary>
    public int PageStart { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="enumerableInput">Enumerable input.</param>
    public ClassEnumerableInputHelper(ClassEnumerableInput<TClass> enumerableInput)
    {
        _enumerableInput = enumerableInput;

        var queryLimit = enumerableInput.QueryContext.GetLimit();
        if (queryLimit.HasValue)
        {
            Limit = Math.Min((int)queryLimit.Value, Limit);
        }
    }

    public delegate Task<(IEnumerable<TClass> Items, bool HasMore)> FetchLimitOffsetDelegate(int offset, int limit,
        CancellationToken cancellationToken = default);

    public delegate Task<(IEnumerable<TClass> Items, bool HasMore)> FetchPagedDelegate(int page, int limit,
        CancellationToken cancellationToken = default);

    public delegate Task<(IEnumerable<TClass> Items, bool HasMore)> FetchUntilFlagDelegate(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch remote source using offset/limit method. The iteration ends if hasMore flag is set to <c>true</c>
    /// or empty result.
    /// </summary>
    /// <param name="action">Action to get new data. It is called using new offset and limit values.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Async enumerable of objects.</returns>
    public IEnumerable<TClass> FetchLimitOffset(
        FetchLimitOffsetDelegate action,
        CancellationToken cancellationToken = default)
    {
        int offset = 0;
        int fetchedCount;
        bool hasMore;
        do
        {
            Logger.Instance.Debug($"Run with offset {offset} and limit {Limit}.", GetType().Name);
            var data = action(offset, Limit, cancellationToken).GetAwaiter().GetResult();
            fetchedCount = 0;
            foreach (var item in data.Items)
            {
                fetchedCount++;
                yield return item;
            }
            offset += Limit;
            hasMore = data.HasMore;
        }
        while (fetchedCount > 0 && hasMore);
    }

    /// <summary>
    /// Fetch remote source using paged method. The iteration ends if hasMore flag is set to <c>true</c>
    /// or empty result.
    /// </summary>
    /// <param name="action">Action to get new data. It is called using new page values.</param>
    /// <returns>Async enumerable of objects.</returns>
    public IEnumerable<TClass> FetchPaged(FetchPagedDelegate action)
    {
        int page = PageStart;
        int fetchedCount;
        bool hasMore;
        do
        {
            Logger.Instance.Debug($"Run with page {page} and limit {Limit}.", GetType().Name);
            var data = action(page, Limit).GetAwaiter().GetResult();
            fetchedCount = 0;
            foreach (var item in data.Items)
            {
                fetchedCount++;
                yield return item;
            }
            page++;
            hasMore = data.HasMore;
        }
        while (fetchedCount > 0 && hasMore);
    }

    /// <summary>
    /// Fetch until specific condition. If the end of iteration is reach the action must
    /// return hasMore flag <c>true</c>.
    /// </summary>
    /// <param name="action">Action to fetch data.</param>
    /// <returns>Async enumerable of objects or null.</returns>
    public IEnumerable<TClass> FetchUntilHasMore(
        FetchUntilFlagDelegate action)
    {
        int fetchedCount;
        bool hasMore;
        do
        {
            var data = action().GetAwaiter().GetResult();
            fetchedCount = 0;
            foreach (var item in data.Items)
            {
                fetchedCount++;
                yield return item;
            }
            hasMore = data.HasMore;
        }
        while (fetchedCount > 0 && hasMore);
    }
}
