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
    public int PageStart { get; set; } = 0;

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

    public delegate Task<IEnumerable<TClass>> FetchLimitOffsetDelegate(int offset, int limit);

    /// <summary>
    /// Fetch remote source using offset/limit method.
    /// </summary>
    /// <param name="action">Action to get new data. It is called using new offset and limit values.</param>
    /// <returns>Async enumerable of objects.</returns>
    public async IAsyncEnumerable<TClass> FetchLimitOffset(
        FetchLimitOffsetDelegate action)
    {
        int offset = 0;
        int fetchedCount;
        do
        {
            Logger.Instance.Debug($"Run with offset {offset} and limit {Limit}.", GetType().Name);
            var list = await action(offset, Limit);
            fetchedCount = 0;
            foreach (var item in list)
            {
                fetchedCount++;
                yield return item;
            }
            offset += Limit;
        }
        while (fetchedCount > 0);
    }

    /// <summary>
    /// Fetch remote source using paged method.
    /// </summary>
    /// <param name="action">Action to get new data. It is called using new page values.</param>
    /// <returns>Async enumerable of objects.</returns>
    public async IAsyncEnumerable<TClass> FetchPaged(
        FetchLimitOffsetDelegate action)
    {
        int page = PageStart;
        int fetchedCount;
        do
        {
            Logger.Instance.Debug($"Run with page {page} and limit {Limit}.", GetType().Name);
            var list = await action(page, Limit);
            fetchedCount = 0;
            foreach (var item in list)
            {
                fetchedCount++;
                yield return item;
            }
            page++;
        }
        while (fetchedCount > 0);
    }
}
