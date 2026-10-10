using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// The class contains various helper methods that simplify remote source
/// iteration (offset, limit, fetch).
/// </summary>
/// <typeparam name="TClass">Source class type.</typeparam>
public class Fetcher<TClass> where TClass : class
{
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(Fetcher<TClass>));

    private int _limit = 50;

    /// <summary>
    /// Default limit to fetch.
    /// </summary>
    public int Limit
    {
        get => _limit;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _limit = value;
        }
    }

    /// <summary>
    /// Start page index. Zero by default.
    /// </summary>
    public int PageStart { get; set; }

    /// <summary>
    /// Stop fetching if no results returned.
    /// </summary>
    public bool StopOnEmptyPage { get; set; } = true;

    /// <summary>
    /// Constructor.
    /// </summary>
    public Fetcher()
    {
    }

    /// <summary>
    /// Single item fetch delegate.
    /// </summary>
    public delegate Task<TClass?> FetchSingleDelegate(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch all items delegate.
    /// </summary>
    public delegate Task<IEnumerable<TClass>> FetchAllDelegate(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch items with limit-offset strategy and HasMore flag.
    /// </summary>
    public delegate Task<(IEnumerable<TClass> Items, bool HasMore)> FetchLimitOffsetFlagDelegate(int limit, int offset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch items with limit-offset strategy.
    /// </summary>
    public delegate Task<IEnumerable<TClass>> FetchLimitOffsetDelegate(int limit, int offset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch items with paged strategy and HasMore flag.
    /// </summary>
    public delegate Task<(IEnumerable<TClass> Items, bool HasMore)> FetchPagedFlagDelegate(int page, int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch items with paged strategy.
    /// </summary>
    public delegate Task<IEnumerable<TClass>> FetchPagedDelegate(int page, int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch items with HasMore flag strategy.
    /// </summary>
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
        var item = await action.Invoke(cancellationToken)
            .ConfigureAwait(false);
        if (item != null)
        {
            yield return item;
        }
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
    /// Fetch remote source using offset/limit method. The iteration ends if hasMore flag is set to <c>false</c>
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
            var data = await action(Limit, offset, cancellationToken)
                .ConfigureAwait(false);
            fetchedCount = 0;
            foreach (var item in data.Items)
            {
                fetchedCount++;
                yield return item;
            }
            offset += fetchedCount;
            hasMore = data.HasMore;
        }
        while (hasMore && fetchedCount > 0);
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
            var data = await action.Invoke(limit, offset, ct)
                .ConfigureAwait(false);
            var items = data as ICollection<TClass> ?? data.ToList();
            return (items, items.Count >= limit);
        }, cancellationToken);
    }

    /// <summary>
    /// Fetch remote source using paged method. The iteration ends if hasMore flag is set to <c>false</c>
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
            var data = await action(page, Limit, cancellationToken)
                .ConfigureAwait(false);
            fetchedCount = 0;
            foreach (var item in data.Items)
            {
                fetchedCount++;
                yield return item;
            }
            page++;
            hasMore = data.HasMore;
        }
        while (hasMore && (fetchedCount > 0 || !StopOnEmptyPage));
    }

    /// <summary>
    /// Fetch remote source using paged method.
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
            var data = await action.Invoke(page, limit, ct)
                .ConfigureAwait(false);
            var enumerable = data as ICollection<TClass> ?? data.ToList();
            var hasMore = enumerable.Count >= limit;
            return (enumerable, hasMore);
        }, cancellationToken);
    }

    /// <summary>
    /// Fetch until specific condition. If the end of iteration is reach the action must
    /// return hasMore flag <c>false</c>.
    /// </summary>
    /// <param name="action">Action to fetch data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>Async enumerable of objects.</returns>
    public async IAsyncEnumerable<TClass> FetchUntilHasMoreAsync(
        FetchUntilFlagDelegate action,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int fetchedCount;
        bool hasMore;
        do
        {
            var data = await action(cancellationToken)
                .ConfigureAwait(false);
            fetchedCount = 0;
            foreach (var item in data.Items)
            {
                fetchedCount++;
                yield return item;
            }
            hasMore = data.HasMore;
        }
        while (hasMore && (fetchedCount > 0 || !StopOnEmptyPage));
    }
}
