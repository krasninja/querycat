using System.Collections;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Inputs;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// Create <see cref="IRowsInput" /> instances for every FROM clause. It will call the function and opens rows input.
/// So after that visitor all node types will be set. The <see cref="IRowsInput" /> input will be put into
/// <see cref="SelectTableFunctionNode" /> node with AstAttributeKeys.RowsInputKey key.
/// </summary>
internal sealed class CreateRowsInputVisitor : AstVisitor
{
    private readonly IExecutionThread<ExecutionOptions> _executionThread;
    private readonly SelectCommandContext _context;
    private readonly ResolveTypesVisitor _resolveTypesVisitor;
    private readonly CreateDelegateVisitorWithValueStore _createDelegateVisitor;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(CreateRowsInputVisitor));

    public CreateRowsInputVisitor(IExecutionThread<ExecutionOptions> executionThread, SelectCommandContext context)
    {
        _executionThread = executionThread;
        _context = context;
        AstTraversal.TypesToIgnore.Add(typeof(SelectQuerySpecificationNode));
        AstTraversal.TypesToIgnore.Add(typeof(SelectQueryCombineNode));
        AstTraversal.AcceptBeforeIgnore = true;

        _resolveTypesVisitor = new ResolveTypesVisitor(executionThread);
        if (_context.Parent != null)
        {
            _resolveTypesVisitor = new SelectResolveTypesVisitor(_executionThread, _context.Parent);
        }
        _createDelegateVisitor = new CreateDelegateVisitorWithValueStore(executionThread, context);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectTableFunctionNode node, CancellationToken cancellationToken)
    {
        var rowsInput = await VisitFunctionNodeInternalAsync(node.TableFunctionNode, null, node.Alias, cancellationToken);
        if (rowsInput == null)
        {
            throw new QueryCatException(Resources.Errors.InvalidRowsInput);
        }
        node.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);
        node.TableFunctionNode.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
    {
        var selectTableFunc = AstTraversal.GetFirstParent<SelectTableFunctionNode>();
        if (selectTableFunc != null)
        {
            var rowsInput = await VisitFunctionNodeInternalAsync(node, null, string.Empty, cancellationToken);
            node.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);
        }
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        var value = await GetIdentifierValueAsync(node, cancellationToken);
        await VisitIdentifierNodeInternalAsync(node, value, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectIdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        var value = await GetIdentifierValueAsync(node, cancellationToken);
        var rowsInput = await VisitIdentifierNodeInternalAsync(node, value, cancellationToken);
        if (rowsInput != null)
        {
            return;
        }

        // Case: we select from string variable, that contains file name, f.e. "DECLARE X := '1.csv'; SELECT * FROM x".
        var internalValueType = value.Type;
        if (internalValueType == DataType.String
            && !string.IsNullOrEmpty(value.AsStringUnsafe)
            && AstTraversal.GetFirstParent<SelectTableReferenceListNode>() != null)
        {
            rowsInput = await RowsInputFactory.CreateInputSourceFromStringVariableAsync(
                value.AsStringUnsafe,
                _executionThread,
                _createDelegateVisitor,
                node.Format,
                cancellationToken);
            node.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);
        }
    }

    private async ValueTask<VariantValue> GetIdentifierValueAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        if (!_context.CapturedScope.TryGetVariable(node.Name, out var value))
        {
            return VariantValue.Null;
        }
        if (node.HasSelectors)
        {
            var @delegate = await _createDelegateVisitor.RunAndReturnAsync(node, cancellationToken);
            return await @delegate.InvokeAsync(_executionThread, cancellationToken);
        }

        return value;
    }

    private async ValueTask<IRowsInput?> VisitIdentifierNodeInternalAsync(
        IdentifierExpressionNode node,
        VariantValue value,
        CancellationToken cancellationToken)
    {
        if (value.IsNull)
        {
            return null;
        }

        // Case: we select from a variable that already contains an iterator.
        if (value.Type == DataType.Object && value.AsObjectUnsafe != null)
        {
            var rowsInput = await CreateInputSourceFromObjectVariableAsync(_context, value.AsObjectUnsafe, cancellationToken);
            node.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);
            return rowsInput;
        }

        return null;
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectTableValuesNode node, CancellationToken cancellationToken)
    {
        var func = await new SelectCreateDelegateVisitor(_executionThread, _context)
            .RunAndReturnAsync(node, cancellationToken);
        var rowsFrame = (await func.InvokeAsync(_executionThread, cancellationToken)).AsRequired<RowsFrame>();
        var rowsInput = new RowsIteratorInput(rowsFrame.GetIterator());
        node.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);
    }

    private async ValueTask<IRowsInput?> VisitFunctionNodeInternalAsync(
        FunctionCallNode node,
        FunctionCallNode? formatNode,
        string alias,
        CancellationToken cancellationToken)
    {
        // If we already have rows input assign - do not create a new one.
        var rowsInput = node.GetAttribute<IRowsInput>(AstAttributeKeys.RowsInputKey);
        if (rowsInput != null)
        {
            return rowsInput;
        }

        // Determine types for the node.
        await _resolveTypesVisitor.RunAsync(node, cancellationToken);

        var rowsFactory = new RowsInputFactory(_context, alias, formatNode);
        var isVary = node.Arguments.Count > 0 && node.GetAllChildren<IdentifierExpressionNode>().Any();
        // isVary mean that input function refers to other columns/variables that might be changed.
        // For them, we create the special kind of iterator.
        var inputContext = isVary
            ? await CreateVaryInputContextAsync(node, rowsFactory, cancellationToken)
            : await CreateInputContextAsync(node, rowsFactory, cancellationToken);
        if (inputContext == null)
        {
            return null;
        }
        inputContext.Alias = alias;

        SetAlias(inputContext.RowsInput, alias);
        _context.AddInput(inputContext);

        return inputContext.RowsInput;
    }

    private async ValueTask<SelectCommandInputContext?> CreateInputContextAsync(
        FunctionCallNode functionCallNode,
        RowsInputFactory rowsInputFactory,
        CancellationToken cancellationToken)
    {
        var @delegate = await _createDelegateVisitor.RunAndReturnAsync(functionCallNode, cancellationToken);
        var source = await @delegate.InvokeAsync(_executionThread, cancellationToken);
        if (@delegate is FunctionCallFuncUnitDecorator callFuncUnitDecorator)
        {
            callFuncUnitDecorator.CacheEnabled = false;
        }
        return await rowsInputFactory.CreateRowsInputAsync(source, _executionThread, cancellationToken);
    }

    private async ValueTask<SelectCommandInputContext?> CreateVaryInputContextAsync(
        FunctionCallNode functionCallNode,
        RowsInputFactory rowsInputFactory,
        CancellationToken cancellationToken)
    {
        var @delegate = await _createDelegateVisitor.RunAndReturnAsync(functionCallNode, cancellationToken);
        var store = new FunctionResultStore(
            @delegate,
            functionCallNode.GetRequiredAttribute<FuncUnitCallInfo>(AstAttributeKeys.ArgumentsKey));
        functionCallNode.SetAttribute(AstAttributeKeys.StoreKey, store);
        var source = (await store.CallAsync(_executionThread, cancellationToken)).Value;
        var context = await rowsInputFactory.CreateRowsInputAsync(source, _executionThread, cancellationToken);
        if (context == null)
        {
            return null;
        }
        var varyingRowsInput = new VaryingRowsInput(_executionThread, context.RowsInput, store, rowsInputFactory,
            context.InputQueryContext);
        context.RowsInput = varyingRowsInput;
        if (@delegate is FunctionCallFuncUnitDecorator callFuncUnitDecorator)
        {
            callFuncUnitDecorator.CacheEnabled = false;
        }
        return context;
    }

    private async Task<IRowsInput> CreateInputSourceFromObjectVariableAsync(
        SelectCommandContext currentContext,
        object objVariable,
        CancellationToken cancellationToken)
    {
        IRowsInput? rowsInputResult = null;
        if (objVariable is IRowsInput rowsInput)
        {
            currentContext.AddInput(new SelectCommandInputContext(rowsInput)
            {
                IsVariableBound = true,
            });
            await rowsInput.OpenAsync(cancellationToken);
            _logger.LogDebug("Open rows input {RowsInput} from variable.", rowsInput);
            rowsInputResult = rowsInput;
        }
        if (objVariable is IRowsIterator rowsIterator)
        {
            rowsInput = new RowsIteratorInput(rowsIterator);
            currentContext.AddInput(new SelectCommandInputContext(rowsInput)
            {
                IsVariableBound = true,
            });
            rowsInputResult = rowsInput;
        }
        if (objVariable is IEnumerable enumerable && enumerable.GetType().IsGenericType)
        {
#pragma warning disable IL2072
            rowsInput = new CollectionInput(TypeUtils.GetUnderlyingType(enumerable), enumerable);
#pragma warning restore IL2072
            currentContext.AddInput(new SelectCommandInputContext(rowsInput));
            rowsInputResult = rowsInput;
        }

        if (rowsInputResult == null)
        {
            throw new QueryCatException(
                string.Format(Resources.Errors.CannotCreateRowsInputFromType, objVariable.GetType().Name));
        }

        return rowsInputResult;
    }

    private static void SetAlias(IRowsInput input, string alias)
    {
        if (string.IsNullOrEmpty(alias))
        {
            return;
        }
        foreach (var column in input.Columns)
        {
            column.SourceName = alias;
        }
    }

    private sealed class FunctionCallFuncUnitDecorator(
        IFuncUnit func,
        int nodeId,
        IDictionary<int, VariantValue> cacheMap) : IFuncUnit
    {
        private bool _cacheEnabled = true;

        /// <inheritdoc />
        public DataType OutputType => func.OutputType;

        public bool CacheEnabled
        {
            get => _cacheEnabled;
            set
            {
                _cacheEnabled = value;
            }
        }

        /// <inheritdoc />
        public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        {
            if (!_cacheEnabled)
            {
                return await func.InvokeAsync(thread, cancellationToken);
            }

            if (cacheMap.TryGetValue(nodeId, out var value))
            {
                return value;
            }
            value = await func.InvokeAsync(thread, cancellationToken);
            if (value.Type == DataType.Object)
            {
                cacheMap[nodeId] = value;
            }
            return value;
        }
    }

    /// <summary>
    /// Create delegate and cache created object. When we traverse the tree like below:
    ///
    /// SELECT * FROM cache_input(read_file('1.csv') FORMAT csv();
    /// SelectTableFunctionNode (n1)
    ///                     \
    ///                     |--------------------------------------
    ///                     |                                     \
    ///                     FunctionCallNode (cache_input(), n2)   FunctionCallNode (arg csv(), n3)
    ///                                  \
    ///                                  FunctionCallNode (arg read_file(), n4)
    ///
    /// When we reach n1 we run CreateDelegateVisitor and call all other functions. Then we reach n2 (n4) and call them
    /// again so we create new rows input. To prevent duplicate objects creation, we override FunctionCallNode
    /// and return the existing object instead of creation of a new one.
    /// </summary>
    private sealed class CreateDelegateVisitorWithValueStore : SelectCreateDelegateVisitor
    {
        /// <summary>
        /// NodeId -> Object map.
        /// </summary>
        private readonly Dictionary<int, VariantValue> _nodeIdFuncMap = new();

        /// <inheritdoc />
        public CreateDelegateVisitorWithValueStore(IExecutionThread<ExecutionOptions> thread, SelectCommandContext commandContext)
            : base(thread, commandContext)
        {
        }

        /// <inheritdoc />
        public override async ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
        {
            await base.VisitAsync(node, cancellationToken);
            var func = NodeIdFuncMap[node.Id];
            NodeIdFuncMap[node.Id] = new FunctionCallFuncUnitDecorator(func, node.Id, _nodeIdFuncMap);
        }
    }
}
