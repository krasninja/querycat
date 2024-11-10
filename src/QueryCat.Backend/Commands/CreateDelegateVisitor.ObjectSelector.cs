using System.Collections;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal partial class CreateDelegateVisitor
{
    #region Object select strategies

    internal sealed class SelectStrategyContainer(SelectStrategy[] strategies)
    {
        public bool Empty => strategies.Length == 0;

        public bool PushToContext(ObjectSelectorContext context)
        {
            foreach (var strategy in strategies)
            {
                var info = strategy.GetToken(context);
                if (!info.HasValue)
                {
                    return false;
                }
                context.Push(info.Value);
            }
            return true;
        }
    }

    internal abstract class SelectStrategy
    {
        public abstract ObjectSelectorContext.Token? GetToken(ObjectSelectorContext context);
    }

    internal sealed class IdentifierPropertySelectStrategy(string propertyName) : SelectStrategy
    {
        /// <inheritdoc />
        public override ObjectSelectorContext.Token? GetToken(ObjectSelectorContext context)
        {
            return context.ExecutionThread.ObjectSelector.SelectByProperty(context, propertyName);
        }
    }

    internal sealed class IdentifierIndexSelectStrategy : SelectStrategy
    {
        private readonly object?[] _objectIndexesCache;
        private readonly IFuncUnit[] _actions;

        public IdentifierIndexSelectStrategy(
            IdentifierIndexSelectorNode indexSelectorNode,
            IReadOnlyDictionary<int, IFuncUnit> nodeIdFuncMap)
        {
            _actions = indexSelectorNode.IndexExpressions.Select(n => nodeIdFuncMap[n.Id]).ToArray();
            _objectIndexesCache = new object?[_actions.Length];
        }

        /// <inheritdoc />
        public override ObjectSelectorContext.Token? GetToken(ObjectSelectorContext context)
        {
            var thread = context.ExecutionThread;
            for (var i = 0; i < _actions.Length; i++)
            {
                _objectIndexesCache[i] = Converter.ConvertValue(_actions[i].Invoke(thread), typeof(object));
            }
            var info = thread.ObjectSelector.SelectByIndex(context, _objectIndexesCache);
            // Indexes must be initialized, fix it.
            if (info is { Indexes: null })
            {
                info = info.Value with { Indexes = _objectIndexesCache };
            }
            return info;
        }
    }

    private sealed class IdentifierFilterSelectStrategy : SelectStrategy
    {
        private readonly VariantValueContainer _container;
        private readonly ObjectSelectorContext _idNodeContext;
        private readonly IFuncUnit _action;

        public IdentifierFilterSelectStrategy(
            VariantValueContainer container,
            ObjectSelectorContext idNodeContext,
            IdentifierFilterSelectorNode filterSelectorNode,
            IReadOnlyDictionary<int, IFuncUnit> nodeIdFuncMap)
        {
            _container = container;
            _idNodeContext = idNodeContext;
            _action = nodeIdFuncMap[filterSelectorNode.FilterExpressionNode.Id];
        }

        /// <inheritdoc />
        public override ObjectSelectorContext.Token? GetToken(ObjectSelectorContext context)
        {
            var listResult = GetObjectBySelector_GetFiltered(
                context.ExecutionThread,
                nodeAction: _action,
                container: _container,
                enumerable: _idNodeContext.LastValue as IEnumerable);

            return new ObjectSelectorContext.Token(listResult);
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

                    container.Value = VariantValue.CreateFromObject(enumerator.Current);
                    if (nodeAction.Invoke(thread).AsBoolean)
                    {
                        list.Add(enumerator.Current);
                    }
                }
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }

            return list;
        }
    }

    protected static SelectStrategyContainer GetObjectSelectStrategies(IdentifierExpressionNode idNode, IReadOnlyDictionary<int, IFuncUnit> nodeIdFuncMap)
    {
        var strategies = new SelectStrategy[idNode.SelectorNodes.Length];
        for (var i = 0; i < idNode.SelectorNodes.Length; i++)
        {
            var selector = idNode.SelectorNodes[i];
            if (selector is IdentifierPropertySelectorNode propertySelectorNode)
            {
                strategies[i] = new IdentifierPropertySelectStrategy(propertySelectorNode.PropertyName);
            }
            else if (selector is IdentifierIndexSelectorNode indexSelectorNode)
            {
                strategies[i] = new IdentifierIndexSelectStrategy(indexSelectorNode, nodeIdFuncMap);
            }
            else if (selector is IdentifierFilterSelectorNode filterSelectorNode)
            {
                var container = idNode.GetRequiredAttribute<VariantValueContainer>(ObjectSelectorContainerKey);
                var idNodeContext = idNode.GetRequiredAttribute<ObjectSelectorContext>(ObjectSelectorKey);
                strategies[i] = new IdentifierFilterSelectStrategy(container, idNodeContext, filterSelectorNode, nodeIdFuncMap);
            }
        }
        return new SelectStrategyContainer(strategies);
    }

    #endregion

    #region FuncUnits

    private sealed class ObjectSelectFuncUnit : IFuncUnit
    {
        private readonly SelectStrategyContainer _strategies;
        private readonly ObjectSelectorContext _context;
        private readonly string _variableName;

        /// <inheritdoc />
        public DataType OutputType { get; }

        public ObjectSelectFuncUnit(
            string variableName,
            DataType outputType,
            SelectStrategyContainer strategies,
            ObjectSelectorContext context)
        {
            _strategies = strategies;
            OutputType = outputType;
            _context = context;
            _variableName = variableName;
        }

        /// <inheritdoc />
        public VariantValue Invoke(IExecutionThread thread)
        {
            var startObject = thread.GetVariable(_variableName);
            GetObjectBySelector(thread, _context, startObject, _strategies, out var finalValue);
            return finalValue;
        }
    }

    private sealed class ObjectSelectSpecialFuncUnit : IFuncUnit
    {
        private readonly VariantValueContainer _container;
        private readonly SelectStrategyContainer _strategies;
        private readonly ObjectSelectorContext _context;

        /// <inheritdoc />
        public DataType OutputType => DataType.Dynamic;

        public ObjectSelectSpecialFuncUnit(
            VariantValueContainer container,
            SelectStrategyContainer strategies,
            ObjectSelectorContext context)
        {
            _container = container;
            _strategies = strategies;
            _context = context;
        }

        /// <inheritdoc />
        public VariantValue Invoke(IExecutionThread thread)
        {
            if (GetObjectBySelector(
                    thread,
                    _context,
                    VariantValue.CreateFromObject(_container.Value),
                    _strategies,
                    out var value))
            {
                return value;
            }
            return VariantValue.Null;
        }
    }

    #endregion

    private sealed class VariantValueContainer(object? value = default)
    {
        public object? Value { get; set; } = value;
    }

    protected static bool GetObjectBySelector(
        IExecutionThread thread,
        ObjectSelectorContext context,
        VariantValue value,
        SelectStrategyContainer selectStrategyContainer,
        out VariantValue result)
    {
        try
        {
            context.ExecutionThread = thread;
            var success = GetObjectBySelectorInternal(context, value, selectStrategyContainer, out result);
            context.ExecutionThread = NullExecutionThread.Instance;
            return success;
        }
        catch (Exception e)
        {
            var logger = Application.LoggerFactory.CreateLogger(nameof(GetObjectBySelector));
            logger.LogDebug(e, Resources.Errors.CannotSelectObject);
        }

        result = VariantValue.Null;
        return false;
    }

    private static bool GetObjectBySelectorInternal(
        ObjectSelectorContext context,
        VariantValue value,
        SelectStrategyContainer selectStrategyContainer,
        out VariantValue result)
    {
        result = VariantValue.Null;
        if (selectStrategyContainer.Empty || value.AsObjectUnsafe == null)
        {
            result = value;
            return false;
        }

        context.Push(new ObjectSelectorContext.Token(value.AsObjectUnsafe));
        var hasCompleted = selectStrategyContainer.PushToContext(context);
        if (!hasCompleted)
        {
            return false;
        }

        result = VariantValue.CreateFromObject(context.LastValue);
        return true;
    }
}
