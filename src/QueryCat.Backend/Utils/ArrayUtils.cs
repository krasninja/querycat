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
            return source.Concat([item]).ToArray();
        }
        return source;
    }

    /// <summary>
    /// Calculates hash code of every array element and combines it.
    /// </summary>
    /// <param name="values">Array values.</param>
    /// <typeparam name="T">Array type.</typeparam>
    /// <returns>Calculated hash code.</returns>
    public static int GetHashCode<T>(T[] values)
    {
        var hashCode = default(HashCode);
        Array.ForEach(values, el => hashCode.Add(el));
        return hashCode.ToHashCode();
    }
}
