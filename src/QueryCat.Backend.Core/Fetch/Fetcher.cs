using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// The class contains various helper method to simplifies remote source
/// iteration (offset, limit, fetch).
/// </summary>
/// <typeparam name="TClass">Source class type.</typeparam>
public class Fetcher<TClass> where TClass : class
{
    /// <summary>
    /// Default limit to fetch.
    /// </summary>
    public int Limit { get; set; } = 50;

    /// <summary>
    /// Start page index. Zero by default.
    /// </summary>
    public int PageStart { get; set; }

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(Fetcher<TClass>));

    /// <summary>
    /// Constructor.
    /// </summary>
    public Fetcher()
    {
    }

    public delegate Task<TClass> FetchSingleDelegate(CancellationToken cancellationToken = default);

    public delegate Task<IEnumerable<TClass>> FetchAllDelegate(CancellationToken cancellationToken = default);

    public delegate Task<(IEnumerable<TClass> Items, bool HasMore)> FetchLimitOffsetFlagDelegate(int limit, int offset,
        CancellationToken cancellationToken = default);

    public delegate Task<IEnumerable<TClass>> FetchLimitOffsetDelegate(int limit, int offset,
        CancellationToken cancellationToken = default);

    public delegate Task<(IEnumerable<TClass> Items, bool HasMore)> FetchPagedFlagDelegate(int page, int limit,
        CancellationToken cancellationToken = default);

    public delegate Task<IEnumerable<TClass>> FetchPagedDelegate(int page, int limit,
        CancellationToken cancellationToken = default);

    public delegate Task<(IEnumerable<TClass> Items, bool HasMore)> FetchUntilFlagDelegate(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch single item from remote source.
    /// </summary>
    /// <param name="action">Action to get the data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Single item.</returns>
    public IEnumerable<TClass> FetchOne(
        FetchSingleDelegate action,
        CancellationToken cancellationToken = default)
    {
        var item = AsyncUtils.RunSync(() => action.Invoke(cancellationToken));
        yield return item!;
    }

    /// <summary>
    /// Fetch all items from remote source.
    /// </summary>
    /// <param name="action">Action to get the data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Multiple items.</returns>
    public IEnumerable<TClass> FetchAll(
        FetchAllDelegate action,
        CancellationToken cancellationToken = default)
    {
        return AsyncUtils.RunSync(() => action.Invoke(cancellationToken))!;
    }

    /// <summary>
    /// Fetch remote source using offset/limit method. The iteration ends if hasMore flag is set to <c>true</c>
    /// or empty result.
    /// </summary>
    /// <param name="action">Action to get new data. It is called using new offset and limit values.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Async enumerable of objects.</returns>
    public IEnumerable<TClass> FetchLimitOffset(
        FetchLimitOffsetFlagDelegate action,
        CancellationToken cancellationToken = default)
    {
        var offset = 0;
        int fetchedCount;
        bool hasMore;
        do
        {
            _logger.LogDebug("Run with offset {Offset} and limit {Limit}.", offset, Limit);
            var localOffset = offset;
            var data = AsyncUtils.RunSync(() => action(Limit, localOffset, cancellationToken));
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
    /// Fetch remote source using offset/limit method. The iteration ends if result is empty.
    /// </summary>
    /// <param name="action">Action to get new data. It is called using new offset and limit values.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Async enumerable of objects.</returns>
    public IEnumerable<TClass> FetchLimitOffset(
        FetchLimitOffsetDelegate action,
        CancellationToken cancellationToken = default)
    {
        return FetchLimitOffset(async (limit, offset, ct) =>
        {
            var data = await action.Invoke(limit, offset, ct);
            return (data, true);
        });
    }

    /// <summary>
    /// Fetch remote source using paged method. The iteration ends if hasMore flag is set to <c>true</c>
    /// or empty result.
    /// </summary>
    /// <param name="action">Action to get new data. It is called using new page values.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Async enumerable of objects.</returns>
    public IEnumerable<TClass> FetchPaged(FetchPagedFlagDelegate action,
        CancellationToken cancellationToken = default)
    {
        var page = PageStart;
        int fetchedCount;
        bool hasMore;
        do
        {
            _logger.LogDebug("Run with page {Page} and limit {Limit}.", page, Limit);
            var localPage = page;
            var data = AsyncUtils.RunSync(() => action(localPage, Limit, cancellationToken));
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
    /// Fetch remote source using paged method. The iteration ends if hasMore flag is set to <c>true</c>
    /// or empty result.
    /// </summary>
    /// <param name="action">Action to get new data. It is called using new page values.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Async enumerable of objects.</returns>
    public IEnumerable<TClass> FetchPaged(FetchPagedDelegate action,
        CancellationToken cancellationToken = default)
    {
        return FetchPaged(async (page, limit, ct) =>
        {
            var data = await action.Invoke(page, limit, ct);
            var enumerable = data as ICollection<TClass> ?? data.ToList();
            var hasMore = enumerable.Count >= limit;
            return (enumerable, hasMore);
        }, cancellationToken);
    }

    /// <summary>
    /// Fetch until specific condition. If the end of iteration is reach the action must
    /// return hasMore flag <c>true</c>.
    /// </summary>
    /// <param name="action">Action to fetch data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Async enumerable of objects or null.</returns>
    public IEnumerable<TClass> FetchUntilHasMore(FetchUntilFlagDelegate action,
        CancellationToken cancellationToken = default)
    {
        int fetchedCount;
        bool hasMore;
        do
        {
            var data = AsyncUtils.RunSync(() => action(cancellationToken));
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
