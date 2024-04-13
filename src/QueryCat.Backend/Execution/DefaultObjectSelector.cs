using System.Collections;
using System.Reflection;
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
        var propertyInfo = current.Object.GetType().GetProperty(propertyName);
        if (propertyInfo == null)
        {
            return null;
        }
        var resultObject = propertyInfo.GetValue(current.Object);
        if (resultObject != null)
        {
            return new ObjectSelectorContext.SelectInfo(resultObject, propertyInfo);
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
            if (current.Object is IList list)
            {
                resultObject = list[intIndex];
            }
            else if (current.Object is Array array)
            {
                resultObject = array.GetValue(intIndex);
            }
            else if (current.Object is IEnumerable<object> objectsEnumerable)
            {
                resultObject = objectsEnumerable.ElementAt(intIndex);
            }
            else if (current.Object is IEnumerable enumerable)
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
        if (resultObject == null && current.PropertyInfo != null)
        {
            resultObject = current.PropertyInfo.GetValue(current.Object, indexes);
        }

        if (resultObject != null)
        {
            return new ObjectSelectorContext.SelectInfo(resultObject);
        }

        return null;
    }

    /// <inheritdoc />
    public virtual void SetValue(object obj, object? newValue, PropertyInfo propertyInfo, object?[] indexes)
    {
        if (indexes.Length == 0)
        {
            propertyInfo.SetValue(obj, newValue, indexes);
        }
        else
        {
            if (indexes.Length == 1 && indexes[0] is long longIndex)
            {
                var intIndex = (int)longIndex;
                if (obj is IList list)
                {
                    list[intIndex] = newValue;
                }
                else if (obj is Array array)
                {
                    array.SetValue(array, intIndex);
                }
            }
            else
            {
                propertyInfo.SetValue(obj, newValue, indexes);
            }
        }
    }
}
