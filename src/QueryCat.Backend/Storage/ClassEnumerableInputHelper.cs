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
    /// Constructor.
    /// </summary>
    /// <param name="enumerableInput">Enumerable input.</param>
    public ClassEnumerableInputHelper(ClassEnumerableInput<TClass> enumerableInput)
    {
        _enumerableInput = enumerableInput;
    }

    public delegate Task<IEnumerable<TClass>> FetchLimitOffsetDelegate(int offset, int limit);

    /// <summary>
    /// Fetch remote source using offset/limit method.
    /// </summary>
    /// <param name="action">Action to get new data. It is called using new offset and limit values.</param>
    /// <param name="limit">The limit count (batch size) for every fetch call.</param>
    /// <returns>Async enumerable of objects.</returns>
    public async IAsyncEnumerable<TClass> FetchLimitOffset(
        FetchLimitOffsetDelegate action,
        int limit = 50)
    {
        int offset = 0;
        int fetchedCount;
        do
        {
            var list = await action(offset, limit);
            fetchedCount = 0;
            foreach (var item in list)
            {
                fetchedCount++;
                yield return item;
            }
            offset += limit;
        }
        while (fetchedCount > 0);
    }
}
