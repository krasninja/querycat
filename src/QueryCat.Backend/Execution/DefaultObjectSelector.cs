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
        var lastObject = context.LastValue;
        if (lastObject == null)
        {
            throw new InvalidOperationException("Invalid selector state.");
        }

        var propertyFindOptions = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
        if (CaseInsensitivePropertyName)
        {
            propertyFindOptions |= BindingFlags.IgnoreCase;
        }
        var propertyInfo = lastObject.GetType().GetProperty(propertyName, propertyFindOptions);
        if (propertyInfo == null || !propertyInfo.CanRead)
        {
            return null;
        }

        var resultObject = propertyInfo.GetValue(lastObject);
        return new ObjectSelectorContext.Token(resultObject, propertyInfo);
    }

    /// <inheritdoc />
    public virtual ObjectSelectorContext.Token? SelectByIndex(ObjectSelectorContext context, params object?[] indexes)
    {
        var current = context.Peek();
        object? resultObject = null;

        if (indexes.Length == 1 && indexes[0] != null)
        {
            // Try to get value with GetValue call (dictionary).
            if (resultObject == null && current.Value is IDictionary dictionary)
            {
                // Dictionary.
                if (indexes[0] != null)
                {
                    var keyType = dictionary.GetType().GetGenericArguments()[0];
                    var key = ConvertValue(indexes[0], keyType);
                    if (key != null && dictionary.Contains(key))
                    {
                        resultObject = dictionary[key];
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            // First try to use the most popular case when we have only one integer index.
            if (resultObject == null && TryGetObjectIsIntegerIndex(indexes[0], out var intIndex)
                && intIndex > -1)
            {
                // Array.
                if (current.Value is Array array)
                {
                    if (intIndex < array.Length)
                    {
                        resultObject = array.GetValue(intIndex);
                    }
                    else
                    {
                        return null;
                    }
                }
                // List.
                else if (current.Value is IList list)
                {
                    if (intIndex < list.Count)
                    {
                        resultObject = list[intIndex];
                    }
                    else
                    {
                        return null;
                    }
                }
                // Generic enumerable.
                else if (current.Value is IEnumerable<object> objectsEnumerable)
                {
                    resultObject = objectsEnumerable.ElementAtOrDefault(intIndex);
                }
                // Enumerable.
                else if (current.Value is IEnumerable enumerable)
                {
                    resultObject = GetEnumerableItemByIndex(enumerable, intIndex);
                }
            }
        }

        // Index property.
        if (resultObject == null && current.Value != null && indexes.Length > 0 && indexes.All(i => i != null))
        {
            var indexProperty = current.Value.GetType()
                .GetProperties()
                .FirstOrDefault(p => p.CanRead && IsPropertyMatchesIndexes(p, indexes));
            if (indexProperty != null)
            {
                try
                {
                    resultObject = indexProperty.GetValue(
                        current.Value,
                        PrepareObjectMatchTypes(indexProperty.GetIndexParameters(), indexes));
                }
                // Skip out of range exception.
                catch (TargetInvocationException e) when (e.InnerException is ArgumentOutOfRangeException)
                {
                }
            }
        }

        if (resultObject != null)
        {
            return new ObjectSelectorContext.Token(resultObject, Indexes: indexes);
        }
        return null;
    }

    #region For index properties

    private static bool IsPropertyMatchesIndexes(PropertyInfo propertyInfo, object?[] indexes)
    {
        var indexParameters = propertyInfo.GetIndexParameters();
        if (indexParameters.Length != indexes.Length)
        {
            return false;
        }

        for (var i = 0; i < indexParameters.Length; i++)
        {
            var indexParameterType = indexParameters[i].ParameterType;
            if (indexParameterType == typeof(int) && indexes[i] is long)
            {
                continue;
            }
            if (indexParameterType != indexes[i]?.GetType())
            {
                return false;
            }
        }

        return true;
    }

    private object?[] PrepareObjectMatchTypes(ParameterInfo[] types, object?[] objs)
    {
        if (types.Length != objs.Length)
        {
            return objs;
        }
        var newObjs = objs;

        if (types.Any(t => t.ParameterType == typeof(int)))
        {
            newObjs = new object?[objs.Length];
            for (var i = 0; i < objs.Length; i++)
            {
                if (TryGetObjectIsIntegerIndex(objs[i], out var objInt))
                {
                    newObjs[i] = objInt;
                }
                else
                {
                    newObjs[i] = objs;
                }
            }
        }

        return newObjs;
    }

    #endregion

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
    public virtual bool SetValue(ObjectSelectorContext context, object? newValue)
    {
        if (context.Length < 2)
        {
            return false;
        }

        // In context by that time we should have something like that: [], [], ..., [owner], [owner prop value].
        var token = context.Peek();
        var owner = context.SelectStack[^2].Value;
        var indexes = token.Indexes ?? [];
        var propertyInfo = token.PropertyInfo;

        // No indexes, expression like "User.Name = 'Vladimir'".
        if (propertyInfo != null && propertyInfo.CanWrite)
        {
            propertyInfo.SetValue(
                owner,
                ConvertValue(newValue, propertyInfo.PropertyType),
                indexes);
            return true;
        }

        // Has one index - check list/array/dict case.
        if (indexes.Length == 1)
        {
            // Dictionary.
            if (indexes[0] != null && owner is IDictionary dictionary)
            {
                dictionary[indexes[0]!] = ConvertValue(newValue, TypeUtils.GetUnderlyingType(dictionary));
                return true;
            }
            if (TryGetObjectIsIntegerIndex(indexes[0], out var intIndex))
            {
                // Array.
                if (owner is Array array)
                {
                    array.SetValue(
                        ConvertValue(newValue, TypeUtils.GetUnderlyingType(array)),
                        intIndex);
                    return true;
                }
                // List.
                if (owner is IList list)
                {
                    list[intIndex] = ConvertValue(newValue, TypeUtils.GetUnderlyingType(list));
                    return true;
                }
            }
        }

        // Index property.
        else
        {
            if (propertyInfo == null || !propertyInfo.CanWrite)
            {
                return false;
            }
            propertyInfo.SetValue(
                owner,
                ConvertValue(newValue, propertyInfo.PropertyType),
                indexes);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Convert value to the target type.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <param name="targetType">Target type.</param>
    /// <returns>Converted value or null if cannot convert.</returns>
    protected virtual object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return null;
        }
        if (value.GetType() == targetType)
        {
            return value;
        }
        return Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
    }

    private static bool TryGetObjectIsIntegerIndex(object? obj, out int value)
    {
        if (obj is int intValue)
        {
            value = intValue;
            return true;
        }
        if (obj is long longValue && longValue >= int.MinValue && longValue <= int.MaxValue)
        {
            value = (int)longValue;
            return true;
        }

        value = 0;
        return false;
    }
}
