using System.Collections;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Reflection-based object selector.
/// </summary>
public class DefaultObjectSelector : IObjectSelector
{
    /// <inheritdoc />
    public virtual ObjectSelectorContext.SelectInfo? SelectByProperty(ObjectSelectorContext context, string propertyName)
    {
        var current = context.SelectStack.Peek();
        var propertyInfo = current.ResultObject.GetType().GetProperty(propertyName);
        if (propertyInfo == null)
        {
            return null;
        }
        var resultObject = propertyInfo.GetValue(current.ResultObject);
        if (resultObject != null)
        {
            return new ObjectSelectorContext.SelectInfo(resultObject,
                new ObjectSelectorContext.SelectPropertyInfo(current.ResultObject, propertyInfo));
        }

        return null;
    }

    /// <inheritdoc />
    public virtual ObjectSelectorContext.SelectInfo? SelectByIndex(ObjectSelectorContext context, object?[] indexes)
    {
        var current = context.SelectStack.Peek();
        object? resultObject = null;

        // First try to use the most popular case when we have only one integer index.
        if (indexes.Length == 1 && indexes[0] is long longIndex)
        {
            var intIndex = (int)longIndex;
            if (current.ResultObject is IList list)
            {
                resultObject = list[intIndex];
            }
            else if (current.ResultObject is Array array)
            {
                resultObject = array.GetValue(intIndex);
            }
            else if (current.ResultObject is IEnumerable<object> objectsEnumerable)
            {
                resultObject = objectsEnumerable.ElementAt(intIndex);
            }
            else if (current.ResultObject is IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                for (var i = 0; enumerator.MoveNext(); i++)
                {
                    if (i == intIndex)
                    {
                        resultObject = enumerator.Current;
                    }
                }
                (enumerator as IDisposable)?.Dispose();
            }
        }

        // Then try to get value with GetValue call.
        if (resultObject == null && current.SelectProperty.HasValue)
        {
            resultObject = current.SelectProperty.Value.PropertyInfo.GetValue(current.ResultObject, indexes);
        }

        if (resultObject != null)
        {
            if (current.SelectProperty.HasValue)
            {
                return new ObjectSelectorContext.SelectInfo(resultObject,
                    current.SelectProperty.Value with { Owner = current.ResultObject });
            }
            else
            {
                return new ObjectSelectorContext.SelectInfo(resultObject);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public virtual void SetValue(in ObjectSelectorContext.SelectInfo selectInfo, object? newValue, object?[] indexes)
    {
        if (!selectInfo.SelectProperty.HasValue)
        {
            return;
        }
        var selectPropertyInfo = selectInfo.SelectProperty.Value;

        // No indexes, expression like "User.Name = 'Vladimir'".
        if (indexes.Length == 0)
        {
            selectPropertyInfo.PropertyInfo.SetValue(selectPropertyInfo.Owner, newValue, indexes);
        }
        // Object expression with index.
        else
        {
            // Case "User.Phones[1] = '888'".
            if (indexes.Length == 1 && indexes[0] is long longIndex)
            {
                var intIndex = (int)longIndex;
                if (selectPropertyInfo.Owner is IList list)
                {
                    list[intIndex] = newValue;
                }
                else if (selectPropertyInfo.Owner is Array array)
                {
                    array.SetValue(array, intIndex);
                }
            }
            // General case.
            else
            {
                selectPropertyInfo.PropertyInfo.SetValue(selectPropertyInfo.Owner, newValue, indexes);
            }
        }
    }
}
