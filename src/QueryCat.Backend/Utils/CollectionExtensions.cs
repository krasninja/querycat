namespace QueryCat.Backend.Utils;

/// <summary>
/// Extensions for collections.
/// </summary>
internal static class CollectionExtensions
{
#if NET8_0
    /// <summary>
    /// Add range to list.
    /// </summary>
    /// <param name="list">List.</param>
    /// <param name="source">Source items.</param>
    /// <typeparam name="T">List type.</typeparam>
    public static void AddRange<T>(this List<T> list, params T[] source)
    {
        list.AddRange(source);
    }
#endif
}
