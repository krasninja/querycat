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

        public async ValueTask<bool> PushToContextAsync(ObjectSelectorContext context,
            CancellationToken cancellationToken)
        {
            foreach (var strategy in strategies)
            {
                var info = await strategy.GetTokenAsync(context, cancellationToken);
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
        public abstract ValueTask<ObjectSelectorContext.Token?> GetTokenAsync(ObjectSelectorContext context,
            CancellationToken cancellationToken = default);
    }

    internal sealed class IdentifierPropertySelectStrategy(string propertyName) : SelectStrategy
    {
        /// <inheritdoc />
        public override ValueTask<ObjectSelectorContext.Token?> GetTokenAsync(ObjectSelectorContext context,
            CancellationToken cancellationToken = default)
        {
            return context.ExecutionThread.ObjectSelector.SelectByPropertyAsync(context, propertyName, cancellationToken);
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
        public override async ValueTask<ObjectSelectorContext.Token?> GetTokenAsync(ObjectSelectorContext context,
            CancellationToken cancellationToken = default)
        {
            var thread = context.ExecutionThread;
            for (var i = 0; i < _actions.Length; i++)
            {
                var value = await _actions[i].InvokeAsync(thread, cancellationToken);
                _objectIndexesCache[i] = Converter.ConvertValue(value, typeof(object));
            }
            var info = await thread.ObjectSelector.SelectByIndexAsync(context, _objectIndexesCache, cancellationToken);
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
        public override async ValueTask<ObjectSelectorContext.Token?> GetTokenAsync(ObjectSelectorContext context,
            CancellationToken cancellationToken = default)
        {
            var listResult = await GetObjectBySelector_GetFilteredAsync(
                context.ExecutionThread,
                nodeAction: _action,
                container: _container,
                enumerable: _idNodeContext.LastValue as IEnumerable,
                cancellationToken: cancellationToken);

            return new ObjectSelectorContext.Token(listResult);
        }

        private static async ValueTask<IList<object>> GetObjectBySelector_GetFilteredAsync(
            IExecutionThread thread,
            IFuncUnit nodeAction,
            VariantValueContainer container,
            IEnumerable? enumerable,
            CancellationToken cancellationToken = default)
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
                    if ((await nodeAction.InvokeAsync(thread, cancellationToken)).AsBoolean)
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
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            var startObject = thread.GetVariable(_variableName);
            var result = await GetObjectBySelectorAsync(thread, _context, startObject, _strategies, cancellationToken);
            return result;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(ObjectSelectFuncUnit)}: {_variableName}";
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
        public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            return GetObjectBySelectorAsync(
                    thread,
                    _context,
                    VariantValue.CreateFromObject(_container.Value),
                    _strategies,
                    cancellationToken);
        }
    }

    #endregion

    private sealed class VariantValueContainer(object? value = null)
    {
        public object? Value { get; set; } = value;
    }

    protected static async ValueTask<VariantValue> GetObjectBySelectorAsync(
        IExecutionThread thread,
        ObjectSelectorContext context,
        VariantValue value,
        SelectStrategyContainer selectStrategyContainer,
        CancellationToken cancellationToken = default)
    {
        try
        {
            context.ExecutionThread = thread;
            var result = await GetObjectBySelectorInternalAsync(context, value, selectStrategyContainer,
                cancellationToken);
            context.ExecutionThread = NullExecutionThread.Instance;
            return result;
        }
        catch (Exception e)
        {
            var logger = Application.LoggerFactory.CreateLogger(nameof(GetObjectBySelectorAsync));
            logger.LogDebug(e, Resources.Errors.CannotSelectObject);
        }

        return VariantValue.Null;
    }

    private static async ValueTask<VariantValue> GetObjectBySelectorInternalAsync(
        ObjectSelectorContext context,
        VariantValue value,
        SelectStrategyContainer selectStrategyContainer,
        CancellationToken cancellationToken = default)
    {
        if (selectStrategyContainer.Empty || value.AsObjectUnsafe == null)
        {
            return value;
        }

        context.Push(new ObjectSelectorContext.Token(value.AsObjectUnsafe));
        var hasCompleted = await selectStrategyContainer.PushToContextAsync(context, cancellationToken);
        if (!hasCompleted)
        {
            return VariantValue.Null;
        }

        return VariantValue.CreateFromObject(context.LastValue);
    }
}
