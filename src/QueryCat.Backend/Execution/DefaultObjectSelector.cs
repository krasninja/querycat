using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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
    private static readonly ValueTask<ObjectSelectorContext.Token?> _emptyTokenResult
        = ValueTask.FromResult<ObjectSelectorContext.Token?>(null);

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _typePropertiesMap = new();

    /// <summary>
    /// Ignore string case on property resolve by name.
    /// </summary>
    public bool CaseInsensitivePropertyName { get; init; }

    /// <inheritdoc />
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Object selector works with arbitrary runtime objects whose properties cannot be statically annotated.")]
    public virtual ValueTask<ObjectSelectorContext.Token?> SelectByPropertyAsync(
        ObjectSelectorContext context,
        string propertyName,
        CancellationToken cancellationToken = default)
    {
        var lastObject = context.LastValue;
        if (lastObject == null)
        {
            throw new InvalidOperationException(Resources.Errors.InvalidSelectorState);
        }

        var readableProperties = _typePropertiesMap.GetOrAdd(lastObject.GetType(), GetReadableProperties);
        PropertyInfo? propertyInfo = null;
        foreach (var readableProperty in readableProperties)
        {
            var match = CaseInsensitivePropertyName
                ? readableProperty.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase)
                : readableProperty.Name.Equals(propertyName, StringComparison.InvariantCulture);
            if (match)
            {
                propertyInfo = readableProperty;
                break;
            }
        }
        if (propertyInfo == null || !propertyInfo.CanRead)
        {
            return _emptyTokenResult;
        }

        var resultObject = propertyInfo.GetValue(lastObject);
        var result = new ObjectSelectorContext.Token(resultObject, propertyInfo);
        return ValueTask.FromResult(new ObjectSelectorContext.Token?(result));
    }

    /// <inheritdoc />
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Object selector works with arbitrary runtime objects whose properties cannot be statically annotated.")]
    public virtual ValueTask<ObjectSelectorContext.Token?> SelectByIndexAsync(
        ObjectSelectorContext context,
        object?[] indexes,
        CancellationToken cancellationToken = default)
    {
        var lastObject = context.LastValue;
        if (lastObject == null)
        {
            throw new InvalidOperationException(Resources.Errors.InvalidSelectorState);
        }

        object? resultObject = null;
        var found = false;
        PropertyInfo? indexProperty = null;

        if (indexes.Length == 1 && indexes[0] != null)
        {
            // Try to get value with GetValue call (dictionary).
            if (!found && lastObject is IDictionary dictionary)
            {
                // Dictionary.
                var keyType = GetDictionaryKeyType(dictionary);
                var key = keyType != null ? ConvertValue(indexes[0], keyType) : indexes[0];
                if (key != null && dictionary.Contains(key))
                {
                    resultObject = dictionary[key];
                    found = true;
                }
                else
                {
                    return _emptyTokenResult;
                }
            }

            // First try to use the most popular case when we have only one integer index.
            if (!found && TryGetIntegerIndex(indexes[0], out var intIndex)
                && intIndex > -1)
            {
                // Array.
                if (lastObject is Array array)
                {
                    if (intIndex < array.Length && array.Rank == 1)
                    {
                        resultObject = array.GetValue(intIndex);
                        found = true;
                    }
                    else
                    {
                        return _emptyTokenResult;
                    }
                }
                // List.
                else if (lastObject is IList list)
                {
                    if (intIndex < list.Count)
                    {
                        resultObject = list[intIndex];
                        found = true;
                    }
                    else
                    {
                        return _emptyTokenResult;
                    }
                }
                // Read only list.
                else if (lastObject is IReadOnlyList<object> readOnlyList)
                {
                    if (intIndex < readOnlyList.Count)
                    {
                        resultObject = readOnlyList[intIndex];
                        found = true;
                    }
                    else
                    {
                        return _emptyTokenResult;
                    }
                }
                // Generic enumerable.
                else if (lastObject is IEnumerable<object> objectsEnumerable)
                {
                    resultObject = objectsEnumerable.ElementAtOrDefault(intIndex);
                    found = true;
                }
                // Enumerable.
                else if (lastObject is IEnumerable enumerable)
                {
                    resultObject = GetEnumerableItemByIndex(enumerable, intIndex);
                    found = true;
                }
            }
        }

        // Index property.
        if (!found && indexes.Length > 0 && HasNoNulls(indexes))
        {
            indexProperty = GetIndexProperty(lastObject, indexes);
            if (indexProperty != null)
            {
                try
                {
                    resultObject = indexProperty.GetValue(
                        lastObject,
                        PrepareObjectMatchTypes(indexProperty.GetIndexParameters(), indexes));
                    found = true;
                }
                // Skip out of range exception.
                catch (TargetInvocationException e) when (e.InnerException is ArgumentOutOfRangeException)
                {
                }
            }
        }

        if (found)
        {
            var result = new ObjectSelectorContext.Token(resultObject, indexProperty, indexes);
            return ValueTask.FromResult(new ObjectSelectorContext.Token?(result));
        }
        return _emptyTokenResult;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Requires get properties call.")]
    private static PropertyInfo[] GetReadableProperties(Type type)
        => type.GetProperties().Where(p => p.CanRead).ToArray();

    private static PropertyInfo? GetIndexProperty(object obj, object?[] indexes)
    {
        var props = _typePropertiesMap.GetOrAdd(obj.GetType(), GetReadableProperties);

        foreach (var prop in props)
        {
            if (IsPropertyMatchesIndexes(prop, indexes))
            {
                return prop;
            }
        }
        return null;
    }

    private static bool HasNoNulls(object?[] arr)
    {
        foreach (var el in arr)
        {
            if (el == null)
            {
                return false;
            }
        }
        return true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Dictionary interfaces are preserved by the runtime type.")]
    private static Type? GetDictionaryKeyType(IDictionary dictionary)
    {
        foreach (var @interface in dictionary.GetType().GetInterfaces())
        {
            if (@interface.IsGenericType
                && @interface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                return @interface.GetGenericArguments()[0];
            }
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
            if (!indexParameterType.IsInstanceOfType(indexes[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static object?[] PrepareObjectMatchTypes(ParameterInfo[] parameters, object?[] indexes)
    {
        if (parameters.Length != indexes.Length)
        {
            return indexes;
        }

        object?[]? newIndexes = null;
        for (var i = 0; i < indexes.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(int)
                && indexes[i] is not int
                && TryGetIntegerIndex(indexes[i], out var intIndex))
            {
                if (newIndexes == null)
                {
                    newIndexes = new object?[indexes.Length];
                    Array.Copy(indexes, newIndexes, indexes.Length);
                }
                newIndexes[i] = intIndex;
            }
        }

        return newIndexes ?? indexes;
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

    #endregion

    /// <inheritdoc />
    public virtual ValueTask<bool> SetValueAsync(
        ObjectSelectorContext context,
        object? newValue,
        CancellationToken cancellationToken = default)
    {
        if (context.Length < 2)
        {
            return ValueTask.FromResult(false);
        }

        // In context by that time we should have something like that: [], [], ..., [owner], [owner prop value].
        var token = context.Peek();
        var owner = context.SelectStack[^2].Value;
        var indexes = token.Indexes ?? [];
        var propertyInfo = token.PropertyInfo;

        // No indexes, expression like "User.Name = 'Vladimir'".
        if (propertyInfo != null && propertyInfo.CanWrite)
        {
            var matchTypes = PrepareObjectMatchTypes(propertyInfo.GetIndexParameters(), indexes);
            propertyInfo.SetValue(
                owner,
                ConvertValue(newValue, propertyInfo.PropertyType),
                matchTypes);
            return ValueTask.FromResult(true);
        }

        // Has one index - check list/array/dict case.
        if (indexes.Length == 1)
        {
            // Dictionary.
            if (indexes[0] != null && owner is IDictionary dictionary)
            {
                var keyType = GetDictionaryKeyType(dictionary);
                var key = keyType != null ? ConvertValue(indexes[0], keyType) : indexes[0];
                if (key == null)
                {
                    return ValueTask.FromResult(false);
                }
                dictionary[key] = ConvertValue(newValue, GetElementType(dictionary, true));
            }
            if (TryGetIntegerIndex(indexes[0], out var intIndex))
            {
                // Array.
                if (owner is Array array)
                {
                    if (array.Rank != 1 || intIndex < 0 || intIndex >= array.Length)
                    {
                        return ValueTask.FromResult(false);
                    }
                    array.SetValue(
                        ConvertValue(newValue, GetElementType(array, false)),
                        intIndex);
                    return ValueTask.FromResult(true);
                }
                // List.
                if (owner is IList list)
                {
                    if (intIndex < 0 || intIndex >= list.Count)
                    {
                        return ValueTask.FromResult(false);
                    }
                    list[intIndex] = ConvertValue(newValue, GetElementType(list, false));
                    return ValueTask.FromResult(true);
                }
            }
        }

        // Index property.
        else
        {
            if (propertyInfo == null || !propertyInfo.CanWrite)
            {
                return ValueTask.FromResult(false);
            }
            propertyInfo.SetValue(
                owner,
                ConvertValue(newValue, propertyInfo.PropertyType),
                indexes);
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Collection interfaces are preserved by the runtime type.")]
    private static Type GetElementType(object collection, bool dictionaryValue)
    {
        var type = collection.GetType();
        if (type.IsArray)
        {
            return type.GetElementType()!;
        }
        foreach (var @interface in type.GetInterfaces())
        {
            if (!@interface.IsGenericType)
            {
                continue;
            }
            var definition = @interface.GetGenericTypeDefinition();
            if (dictionaryValue && definition == typeof(IDictionary<,>))
            {
                return @interface.GetGenericArguments()[1];
            }
            if (!dictionaryValue && definition == typeof(IList<>))
            {
                return @interface.GetGenericArguments()[0];
            }
        }
        return typeof(object);   // Non-generic IList/IDictionary - ArrayList, Hashtable.
    }

    /// <summary>
    /// Convert value to the target type.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <param name="targetType">Target type.</param>
    /// <returns>Converted value or null if cannot convert.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Only called for value types, which always have a default constructor.")]
    protected virtual object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                ? Activator.CreateInstance(targetType)
                : null;
        }
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlyingType.IsEnum)
        {
            try
            {
                return value is string str
                    ? Enum.Parse(underlyingType, str, ignoreCase: true)
                    : Enum.ToObject(underlyingType, value);
            }
            catch (Exception e) when (e is ArgumentException or InvalidCastException or OverflowException)
            {
                return null;
            }
        }

        try
        {
            return Convert.ChangeType(value, underlyingType);
        }
        catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException)
        {
            return null;
        }
    }

    private static bool TryGetIntegerIndex(object? obj, out int value)
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
