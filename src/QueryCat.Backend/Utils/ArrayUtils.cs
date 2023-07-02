namespace QueryCat.Backend.Utils;

/// <summary>
/// Array utils.
/// </summary>
internal static class ArrayUtils
{
    /// <summary>
    /// Append source array to another.
    /// </summary>
    /// <param name="source">Source.</param>
    /// <param name="item">Item to append.</param>
    /// <typeparam name="T">Array type.</typeparam>
    /// <returns>Appended array.</returns>
    public static T[] Append<T>(T[] source, T? item)
    {
        if (item != null)
        {
            return source.Concat(new[] { item }).ToArray();
        }
        return source;
    }

    /// <summary>
    /// Append source array to another.
    /// </summary>
    /// <param name="source">Source.</param>
    /// <param name="items">Items to append.</param>
    /// <typeparam name="T">Array type.</typeparam>
    /// <returns>Appended array.</returns>
    public static T[] Append<T>(T[] source, T[]? items)
    {
        if (items == null)
        {
            return source;
        }
        return source.Concat(items).ToArray();
    }
}
