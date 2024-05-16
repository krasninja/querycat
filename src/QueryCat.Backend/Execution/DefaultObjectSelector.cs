using System.Collections;
using System.Reflection;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Reflection-based object selector.
/// </summary>
/// <remarks>
/// Documentation: https://github.com/krasninja/querycat/tree/develop/docs/internal/object-selector.md.
/// </remarks>
public class DefaultObjectSelector : IObjectSelector
{
    /// <summary>
    /// Ignore string case on property resolve by name.
    /// </summary>
    public bool CaseInsensitivePropertyName { get; set; }

    /// <inheritdoc />
    public virtual ObjectSelectorContext.Token? SelectByProperty(ObjectSelectorContext context, string propertyName)
    {
        var previousObject = context.PreviousResult;
        if (previousObject == null)
        {
            throw new InvalidOperationException("Invalid selector state.");
        }

        var propertyFindOptions = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
        if (CaseInsensitivePropertyName)
        {
            propertyFindOptions |= BindingFlags.IgnoreCase;
        }
        var propertyInfo = previousObject.GetType().GetProperty(propertyName, propertyFindOptions);
        if (propertyInfo == null || !propertyInfo.CanRead)
        {
            return null;
        }

        var resultObject = propertyInfo.GetValue(previousObject);
        if (resultObject != null)
        {
            return new ObjectSelectorContext.Token(resultObject, propertyInfo);
        }

        return null;
    }

    /// <inheritdoc />
    public virtual ObjectSelectorContext.Token? SelectByIndex(ObjectSelectorContext context, object?[] indexes)
    {
        var current = context.Peek();
        object? resultObject = null;

        // Try to get value with GetValue call (dictionary).
        if (resultObject == null && current.ResultObject is IDictionary dictionary)
        {
            // Dictionary.
            if (indexes.Length == 1 && indexes[0] != null)
            {
                var keyType = dictionary.GetType().GetGenericArguments()[0];
                var key = ConvertValue(indexes[0], keyType);
                if (key != null)
                {
                    resultObject = dictionary[key];
                }
            }

            // Index property.
            if (resultObject == null && current.PropertyInfo != null
                && current.PropertyInfo.CanRead)
            {
                resultObject = current.PropertyInfo.GetValue(current.ResultObject, indexes);
            }
        }

        // First try to use the most popular case when we have only one integer index.
        if (resultObject == null && indexes.Length == 1 && TryGetObjectIsIntegerIndex(indexes[0], out var intIndex))
        {
            // List.
            if (current.ResultObject is IList list)
            {
                resultObject = list[intIndex];
            }
            // Array.
            else if (current.ResultObject is Array array)
            {
                resultObject = array.GetValue(intIndex);
            }
            // Generic enumerable.
            else if (current.ResultObject is IEnumerable<object> objectsEnumerable)
            {
                resultObject = objectsEnumerable.ElementAt(intIndex);
            }
            // Enumerable.
            else if (current.ResultObject is IEnumerable enumerable)
            {
                resultObject = GetEnumerableItemByIndex(enumerable, intIndex);
            }
        }

        if (resultObject != null)
        {
            return new ObjectSelectorContext.Token(resultObject);
        }

        return null;
    }

    private static object? GetEnumerableItemByIndex(IEnumerable enumerable, int index)
    {
        var enumerator = enumerable.GetEnumerator();
        object? result = null;
        try
        {
            var i = 0;
            while (enumerator.MoveNext())
            {
                if (i++ == index)
                {
                    result = enumerator.Current;
                    break;
                }
            }
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
        return result;
    }

    /// <inheritdoc />
    public virtual bool SetValue(in ObjectSelectorContext.Token token, object owner, object? newValue, object?[] indexes)
    {
        // No indexes, expression like "User.Name = 'Vladimir'".
        if (token.PropertyInfo != null && indexes.Length == 0)
        {
            if (!token.PropertyInfo.CanWrite)
            {
                return false;
            }
            token.PropertyInfo.SetValue(
                owner,
                ConvertValue(newValue, token.PropertyInfo.PropertyType),
                indexes);
            return true;
        }

        // Has one index - check list/array/dict case.
        if (indexes.Length == 1)
        {
            if (TryGetObjectIsIntegerIndex(indexes[0], out var intIndex))
            {
                // List.
                if (owner is IList list)
                {
                    list[intIndex] = ConvertValue(newValue, TypeUtils.GetUnderlyingType(list));
                    return true;
                }
                // Array.
                if (owner is Array array)
                {
                    array.SetValue(
                        ConvertValue(newValue, TypeUtils.GetUnderlyingType(array)),
                        intIndex);
                    return true;
                }
            }
            // Dictionary.
            if (indexes[0] != null && owner is IDictionary dictionary)
            {
                dictionary[indexes[0]!] = ConvertValue(newValue, TypeUtils.GetUnderlyingType(dictionary));
                return true;
            }
        }

        // Index property.
        else
        {
            if (token.PropertyInfo == null || !token.PropertyInfo.CanWrite)
            {
                return false;
            }
            token.PropertyInfo.SetValue(
                owner,
                ConvertValue(newValue, token.PropertyInfo.PropertyType),
                indexes);
            return true;
        }

        return false;
    }

    private static object? ConvertValue(object? value, Type? targetType)
    {
        if (value == null)
        {
            return null;
        }
        if (targetType == null)
        {
            return value;
        }
        if (value.GetType() == targetType)
        {
            return value;
        }
        return Convert.ChangeType(value, targetType);
    }

    private static bool TryGetObjectIsIntegerIndex(object? obj, out int value)
    {
        if (obj is int intValue)
        {
            value = intValue;
            return true;
        }
        if (obj is long longValue && longValue > -1 && longValue <= int.MaxValue)
        {
            value = (int)longValue;
            return true;
        }

        value = 0;
        return false;
    }
}
