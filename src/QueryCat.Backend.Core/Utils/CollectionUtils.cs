namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// Utilities for collections.
/// </summary>
internal static class CollectionUtils
{
    /// <summary>
    /// Ensures that list will have at least the specified size.
    /// </summary>
    /// <param name="list">Instance <see cref="List{T}" />.</param>
    /// <param name="capacity">Target capacity.</param>
    /// <typeparam name="T">List type.</typeparam>
    internal static void EnsureListSize<T>(List<T?> list, int capacity)
    {
        ArgumentNullException.ThrowIfNull(list);
        while (list.Count < capacity)
        {
            list.Add(default);
        }
    }
}
