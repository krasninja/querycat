using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

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
    public async IAsyncEnumerable<TClass> FetchOneAsync(
        FetchSingleDelegate action,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var item = await action.Invoke(cancellationToken);
        yield return item;
    }

    /// <summary>
    /// Fetch all items from remote source.
    /// </summary>
    /// <param name="action">Action to get the data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Multiple items.</returns>
    public async IAsyncEnumerable<TClass> FetchAllAsync(
        FetchAllDelegate action,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = await action.Invoke(cancellationToken)
            .ConfigureAwait(false);
        foreach (var item in result)
        {
            yield return item;
        }
    }

    /// <summary>
    /// Fetch remote source using offset/limit method. The iteration ends if hasMore flag is set to <c>true</c>
    /// or empty result.
    /// </summary>
    /// <param name="action">Action to get new data. It is called using new offset and limit values.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Async enumerable of objects.</returns>
    public async IAsyncEnumerable<TClass> FetchLimitOffsetAsync(
        FetchLimitOffsetFlagDelegate action,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var offset = 0;
        int fetchedCount;
        bool hasMore;
        do
        {
            _logger.LogDebug("Run with offset {Offset} and limit {Limit}.", offset, Limit);
            var localOffset = offset;
            var data = await action(Limit, localOffset, cancellationToken);
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
    public IAsyncEnumerable<TClass> FetchLimitOffsetAsync(
        FetchLimitOffsetDelegate action,
        CancellationToken cancellationToken = default)
    {
        return FetchLimitOffsetAsync(async (limit, offset, ct) =>
        {
            var data = await action.Invoke(limit, offset, ct);
            return (data, true);
        }, cancellationToken);
    }

    /// <summary>
    /// Fetch remote source using paged method. The iteration ends if hasMore flag is set to <c>true</c>
    /// or empty result.
    /// </summary>
    /// <param name="action">Action to get new data. It is called using new page values.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Async enumerable of objects.</returns>
    public async IAsyncEnumerable<TClass> FetchPagedAsync(
        FetchPagedFlagDelegate action,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = PageStart;
        int fetchedCount;
        bool hasMore;
        do
        {
            _logger.LogDebug("Run with page {Page} and limit {Limit}.", page, Limit);
            var localPage = page;
            var data = await action(localPage, Limit, cancellationToken);
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
    public IAsyncEnumerable<TClass> FetchPagedAsync(
        FetchPagedDelegate action,
        CancellationToken cancellationToken = default)
    {
        return FetchPagedAsync(async (page, limit, ct) =>
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
    public async IAsyncEnumerable<TClass> FetchUntilHasMoreAsync(
        FetchUntilFlagDelegate action,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int fetchedCount;
        bool hasMore;
        do
        {
            var data = await action(cancellationToken);
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
