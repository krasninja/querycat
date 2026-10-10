using System.Collections;

namespace QueryCat.Backend;

/// <summary>
/// Type utils.
/// </summary>
internal static class TypeUtils
{
    /// <summary>
    /// Gets the underlying element type for collections and arrays.
    /// </summary>
    /// <param name="obj">The object to analyze.</param>
    /// <returns>The element type, or the object's type if not a collection.</returns>
    internal static Type GetUnderlyingType(object obj) => GetUnderlyingType(obj.GetType());

    /// <summary>
    /// Returns element type for array, value type for dictionary, list type for list.
    /// </summary>
    /// <param name="type">Type to get underlying type.</param>
    /// <returns>Underlying type or current if not generic.</returns>
    internal static Type GetUnderlyingType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType() ?? type;
        }

        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();

            if (typeof(IDictionary).IsAssignableFrom(type) && genericArgs.Length >= 2)
            {
                return genericArgs[1]; // Value type.
            }
            if (typeof(IEnumerable).IsAssignableFrom(type) && genericArgs.Length >= 1)
            {
                return genericArgs[0];
            }
        }

        return type;
    }
}
