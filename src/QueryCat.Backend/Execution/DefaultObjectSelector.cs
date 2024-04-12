using System.Collections;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Reflection-based object selector.
/// </summary>
public class DefaultObjectSelector : IObjectSelector
{
    /// <inheritdoc />
    public virtual void PushObjectByProperty(ObjectSelectorContext context, string propertyName)
    {
        var current = context.SelectStack.Peek();
        var propertyInfo = current.Object.GetType().GetProperty(propertyName);
        if (propertyInfo == null)
        {
            return;
        }
        var resultObject = propertyInfo.GetValue(current.Object);
        if (resultObject != null)
        {
            context.SelectStack.Push(new ObjectSelectorContext.ObjectInfo(resultObject, propertyInfo));
        }
    }

    /// <inheritdoc />
    public virtual void PushObjectByIndex(ObjectSelectorContext context, VariantValue[] indexes)
    {
        var current = context.SelectStack.Peek();
        object? resultObject = null;

        // First try to use the most popular case when we have only one integer index.
        if (indexes.Length == 1 && indexes[0].GetInternalType() == DataType.Integer)
        {
            var intIndex = (int)indexes[0].AsInteger;
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
            resultObject = current.PropertyInfo.GetValue(current.Object, indexes.Select(i => i.AsObject).ToArray());
        }

        if (resultObject != null)
        {
            context.SelectStack.Push(new ObjectSelectorContext.ObjectInfo(resultObject));
        }
    }
}
