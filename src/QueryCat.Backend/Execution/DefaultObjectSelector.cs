using System.Collections;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Reflection-based object selector.
/// </summary>
public class DefaultObjectSelector : IObjectSelector
{
    /// <inheritdoc />
    public virtual ObjectSelectorContext.Token? SelectByProperty(ObjectSelectorContext context, string propertyName)
    {
        var current = context.Peek();
        var propertyInfo = current.ResultObject.GetType().GetProperty(propertyName);
        if (propertyInfo == null)
        {
            return null;
        }
        var resultObject = propertyInfo.GetValue(current.ResultObject);
        if (resultObject != null)
        {
            return new ObjectSelectorContext.Token(resultObject,
                new ObjectSelectorContext.TokenPropertyInfo(current.ResultObject, propertyInfo));
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
            if (resultObject == null && current.SelectProperty.HasValue)
            {
                resultObject = current.SelectProperty.Value.PropertyInfo.GetValue(current.ResultObject, indexes);
            }
        }

        // First try to use the most popular case when we have only one integer index.
        if (resultObject == null && indexes.Length == 1 && indexes[0] is long longIndex)
        {
            var intIndex = (int)longIndex;
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
                var enumerator = enumerable.GetEnumerator();
                for (var i = 0; enumerator.MoveNext(); i++)
                {
                    if (i == intIndex)
                    {
                        resultObject = enumerator.Current;
                        break;
                    }
                }
                (enumerator as IDisposable)?.Dispose();
            }
        }

        if (resultObject != null)
        {
            if (current.SelectProperty.HasValue)
            {
                return new ObjectSelectorContext.Token(resultObject,
                    current.SelectProperty.Value with { Owner = current.ResultObject });
            }
            else
            {
                return new ObjectSelectorContext.Token(resultObject);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public virtual bool SetValue(in ObjectSelectorContext.Token token, object? newValue, object?[] indexes)
    {
        if (!token.SelectProperty.HasValue)
        {
            return false;
        }
        var selectPropertyInfo = token.SelectProperty.Value;
        return SetValueInternal(selectPropertyInfo, newValue, indexes);
    }

    private bool SetValueInternal(in ObjectSelectorContext.TokenPropertyInfo selectPropertyInfo, object? newValue, object?[] indexes)
    {
        // No indexes, expression like "User.Name = 'Vladimir'".
        if (indexes.Length == 0)
        {
            selectPropertyInfo.PropertyInfo.SetValue(
                selectPropertyInfo.Owner,
                ConvertValue(newValue, selectPropertyInfo.PropertyInfo.PropertyType),
                indexes);
            return true;
        }

        if (indexes.Length == 1)
        {
            if (indexes[0] is long longIndex)
            {
                var intIndex = (int)longIndex;
                // List.
                if (selectPropertyInfo.Owner is IList list)
                {
                    list[intIndex] = ConvertValue(newValue, TypeUtils.GetUnderlyingType(list));
                    return true;
                }
                // Array.
                if (selectPropertyInfo.Owner is Array array)
                {
                    array.SetValue(
                        ConvertValue(newValue, TypeUtils.GetUnderlyingType(array)),
                        intIndex);
                    return true;
                }
            }
            // Dictionary.
            if (indexes[0] != null && selectPropertyInfo.Owner is IDictionary dictionary)
            {
                dictionary[indexes[0]!] = ConvertValue(newValue, TypeUtils.GetUnderlyingType(dictionary));
                return true;
            }
        }
        // Index property.
        else
        {
            selectPropertyInfo.PropertyInfo.SetValue(
                selectPropertyInfo.Owner,
                ConvertValue(newValue, selectPropertyInfo.PropertyInfo.PropertyType),
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
        return Convert.ChangeType(value, targetType);
    }
}
