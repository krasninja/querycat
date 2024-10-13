using System.Collections;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal partial class CreateDelegateVisitor
{
    private sealed class VariantValueContainer(object? value = default)
    {
        public object? Value { get; set; } = value;
    }

    protected bool GetObjectBySelector(
        IExecutionThread thread,
        ObjectSelectorContext context,
        VariantValue value,
        IdentifierExpressionNode idNode,
        out VariantValue result)
    {
        try
        {
            return GetObjectBySelectorInternal(thread, context, value, idNode, out result);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, Resources.Errors.CannotSelectObject);
        }

        result = VariantValue.Null;
        return false;
    }

    private bool GetObjectBySelectorInternal(
        IExecutionThread thread,
        ObjectSelectorContext context,
        VariantValue value,
        IdentifierExpressionNode idNode,
        out VariantValue result)
    {
        result = VariantValue.Null;
        if (!idNode.HasSelectors || value.AsObjectUnsafe == null)
        {
            result = value;
            return false;
        }

        context.Push(new ObjectSelectorContext.Token(value.AsObjectUnsafe));
        foreach (var selector in idNode.SelectorNodes)
        {
            ObjectSelectorContext.Token? info = null;

            if (selector is IdentifierPropertySelectorNode propertySelectorNode)
            {
                info = thread.ObjectSelector.SelectByProperty(context, propertySelectorNode.PropertyName);
            }
            else if (selector is IdentifierIndexSelectorNode indexSelectorNode)
            {
                var indexObjects = GetObjectIndexesSelector(thread, indexSelectorNode);
                info = thread.ObjectSelector.SelectByIndex(context, indexObjects);
                // Indexes must be initialized, fix it.
                if (info is { Indexes: null })
                {
                    info = info.Value with { Indexes = indexObjects };
                }
            }
            else if (selector is IdentifierFilterSelectorNode filterSelectorNode)
            {
                var container = idNode.GetRequiredAttribute<VariantValueContainer>(ObjectSelectorContainerKey);
                var idNodeContext = idNode.GetRequiredAttribute<ObjectSelectorContext>(ObjectSelectorKey);

                var listResult = GetObjectBySelector_GetFiltered(
                    thread,
                    nodeAction: NodeIdFuncMap[filterSelectorNode.FilterExpressionNode.Id],
                    container: container,
                    enumerable: idNodeContext.LastValue as IEnumerable);

                info = new ObjectSelectorContext.Token(listResult);
            }

            if (!info.HasValue)
            {
                return false;
            }
            context.Push(info.Value);
        }

        result = VariantValue.CreateFromObject(context.LastValue);
        return true;
    }

    protected object?[] GetObjectIndexesSelector(IExecutionThread thread, IdentifierIndexSelectorNode indexSelectorNode)
    {
        if (indexSelectorNode.IndexExpressions.Length == 0)
        {
            return [];
        }
        if (!_objectIndexesCache.TryGetValue(indexSelectorNode, out var indexes))
        {
            indexes = new object?[indexSelectorNode.IndexExpressions.Length];
            _objectIndexesCache.Add(indexSelectorNode, indexes);
        }

        for (var i = 0; i < indexSelectorNode.IndexExpressions.Length; i++)
        {
            var indexExpression = indexSelectorNode.IndexExpressions[i];
            indexes[i] = Converter.ConvertValue(NodeIdFuncMap[indexExpression.Id].Invoke(thread), typeof(object));
        }
        return indexes;
    }

    private static IList<object> GetObjectBySelector_GetFiltered(
        IExecutionThread thread,
        IFuncUnit nodeAction,
        VariantValueContainer container,
        IEnumerable? enumerable)
    {
        if (enumerable == null)
        {
            return [];
        }

        var selectContext = new ObjectSelectorContext();
        var enumerator = enumerable.GetEnumerator();

        var list = new List<object>();
        try
        {
            while (enumerator.MoveNext())
            {
                if (enumerator.Current == null)
                {
                    continue;
                }

                selectContext.Push(new ObjectSelectorContext.Token(enumerator.Current));
                container.Value = VariantValue.CreateFromObject(enumerator.Current);
                if (nodeAction.Invoke(thread).AsBoolean)
                {
                    list.Add(enumerator.Current);
                }

                selectContext.Clear();
            }
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }

        return list;
    }
}
