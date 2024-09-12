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

    /// <summary>
    /// Returns true if all items of both arrays are equal.
    /// </summary>
    /// <param name="source">Source array.</param>
    /// <param name="dest">Destination array.</param>
    /// <typeparam name="T">Type.</typeparam>
    /// <returns>True if all items are equal, false otherwise.</returns>
    public static bool EqualsAll<T>(T[]? source, T[]? dest)
    {
        if (source == null && dest == null)
        {
            return true;
        }
        source ??= [];
        dest ??= [];
        if (source.Length != dest.Length)
        {
            return false;
        }
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] == null && dest[i] == null)
            {
                continue;
            }
            if (source[i] == null && dest[i] != null)
            {
                return false;
            }
            if (!source[i]!.Equals(dest[i]))
            {
                return false;
            }
        }
        return true;
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
