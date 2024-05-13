using System.Collections;

namespace QueryCat.Backend;

/// <summary>
/// Type utils.
/// </summary>
internal static class TypeUtils
{
    internal static Type GetUnderlyingType(object obj) => GetUnderlyingType(obj.GetType());

    /// <summary>
    /// Returns element type for array, value type for dictionary, list type for list.
    /// </summary>
    /// <param name="type">Type to get underlying type.</param>
    /// <returns>Underlying type or current if not generic.</returns>
    internal static Type GetUnderlyingType(Type type)
    {
        if (type == typeof(Array))
        {
            return type.GetElementType()!;
        }

        if (type.IsGenericType)
        {
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return type.GetGenericArguments()[0];
            }
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return type.GetGenericArguments()[1];
            }
        }

        return type;
    }
}
