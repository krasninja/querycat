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
    private readonly RowsInputFactory _rowsInputFactory;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(CreateRowsInputVisitor));

    public CreateRowsInputVisitor(IExecutionThread<ExecutionOptions> executionThread, SelectCommandContext context)
    {
        _executionThread = executionThread;
        _context = context;
        AstTraversal.TypesToIgnore.Add(typeof(SelectQuerySpecificationNode));
        AstTraversal.TypesToIgnore.Add(typeof(SelectQueryCombineNode));
        AstTraversal.AcceptBeforeIgnore = true;

        _resolveTypesVisitor = new SelectResolveTypesVisitor(executionThread, context);
        if (_context.Parent != null)
        {
            _resolveTypesVisitor = new SelectResolveTypesVisitor(_executionThread, _context.Parent);
        }
        _createDelegateVisitor = new CreateDelegateVisitorWithValueStore(executionThread, context);
        _rowsInputFactory = new RowsInputFactory(_context);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectTableFunctionNode node, CancellationToken cancellationToken)
    {
        var rowsInputContext = await VisitFunctionNodeInternalAsync(node.TableFunctionNode, null, node.Alias, cancellationToken);
        if (rowsInputContext == null)
        {
            throw new QueryCatException(Resources.Errors.InvalidRowsInput);
        }
        node.SetAttribute(AstAttributeKeys.RowsInputContextKey, rowsInputContext);
        node.TableFunctionNode.SetAttribute(AstAttributeKeys.RowsInputContextKey, rowsInputContext);
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
    {
        var selectTableFunc = AstTraversal.GetFirstParent<SelectTableFunctionNode>();
        if (selectTableFunc != null)
        {
            var rowsInputContext = await VisitFunctionNodeInternalAsync(node, null, string.Empty, cancellationToken);
            node.SetAttribute(AstAttributeKeys.RowsInputContextKey, rowsInputContext);
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
        if (value.IsNull)
        {
            var inputContext = await GetInputSourceValueAsync(node, cancellationToken);
            if (inputContext != null)
            {
                inputContext.IsVary = true;
                value = VariantValue.CreateFromObject(inputContext.RowsInput);
            }
        }
        await VisitIdentifierNodeInternalAsync(node, value, cancellationToken);
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

    private async ValueTask<SelectInputQueryContext?> GetInputSourceValueAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        if (!_context.TryGetInputSourceByName(node.TableFieldName, node.TableSourceName, out var result)
            || result == null
            || result.InputQueryContext == null)
        {
            return null;
        }
        var columnIndex = result.InputQueryContext.RowsInput.GetColumnIndexByName(node.TableFieldName,
            node.TableSourceName);
        if (columnIndex < 0)
        {
            return null;
        }
        if (result.InputQueryContext.RowsInput is not PrefetchRowsInput)
        {
            result.InputQueryContext.RowsInput = await PrefetchRowsInput.CreateAsync(
                result.InputQueryContext.RowsInput,
                cancellationToken);
        }
        if (result.InputQueryContext.RowsInput.ReadValue(columnIndex, out var value) != ErrorCode.OK)
        {
            return null;
        }

        var funcUnit = new FuncUnitRowsInputColumn(result.InputQueryContext.RowsInput, columnIndex);
        var store = new FunctionResultStore(funcUnit, new FuncUnitCallInfo(funcUnit));
        var rowsInputContextValue = (await store.CallAsync(_executionThread, cancellationToken)).Value;

        var alias = node is ISelectAliasNode selectAliasNode ? selectAliasNode.Alias : string.Empty;
        var rowsInputContext = await _rowsInputFactory.CreateRowsInputAsync(
            rowsInputContextValue,
            alias,
            _executionThread,
            formatNode: null,
            resolveStringAsSource: true,
            cancellationToken);
        if (rowsInputContext == null)
        {
            return null;
        }

        rowsInputContext.RowsInput = new VaryingRowsInput(
            _executionThread,
            rowsInputContext.RowsInput,
            store,
            _rowsInputFactory,
            rowsInputContext);

        return rowsInputContext;
    }

    private async ValueTask<SelectInputQueryContext?> VisitIdentifierNodeInternalAsync(
        IdentifierExpressionNode node,
        VariantValue value,
        CancellationToken cancellationToken)
    {
        if (value.IsNull)
        {
            return null;
        }

        var isPartOfFromClause = AstTraversal.GetParents<SelectTableReferenceListNode>().Any();
        var rowsInputContext = await _rowsInputFactory.CreateRowsInputAsync(
            value, _executionThread, resolveStringAsSource: isPartOfFromClause, cancellationToken);

        if (rowsInputContext != null)
        {
            rowsInputContext.IsVariableBound = true;
            _context.AddInput(rowsInputContext);
            await rowsInputContext.RowsInput.OpenAsync(cancellationToken);
            _logger.LogDebug("Open rows input {RowsInput}.", rowsInputContext.RowsInput);
            node.SetAttribute(AstAttributeKeys.RowsInputContextKey, rowsInputContext);
            return rowsInputContext;
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
        var context = new SelectInputQueryContext(rowsInput);
        _context.AddInput(context);
        node.SetAttribute(AstAttributeKeys.RowsInputContextKey, context);
    }

    private async ValueTask<SelectInputQueryContext?> VisitFunctionNodeInternalAsync(
        FunctionCallNode node,
        FunctionCallNode? formatNode,
        string alias,
        CancellationToken cancellationToken)
    {
        // If we already have rows input assign - do not create a new one.
        var rowsInputContext = node.GetAttribute<SelectInputQueryContext>(AstAttributeKeys.RowsInputContextKey);
        if (rowsInputContext != null)
        {
            return rowsInputContext;
        }

        // Determine types for the node.
        await _resolveTypesVisitor.RunAsync(node, cancellationToken);

        var isVary = node.Arguments.Count > 0 && node.GetAllChildren<IdentifierExpressionNode>().Any();
        // isVary mean that input function refers to other columns/variables that might be changed.
        // For them, we create the special kind of iterator.
        var inputContext = isVary
            ? await CreateVaryInputContextAsync(node, formatNode, alias, cancellationToken)
            : await CreateInputContextAsync(node, formatNode, alias, cancellationToken);
        if (inputContext == null)
        {
            return null;
        }
        inputContext.Alias = alias;
        inputContext.IsVary = isVary;

        await inputContext.RowsInput.OpenAsync(cancellationToken);
        _logger.LogDebug("Open rows input {RowsInput}.", inputContext.RowsInput);

        SetAlias(inputContext.RowsInput, alias);
        _context.AddInput(inputContext);

        return inputContext;
    }

    private async ValueTask<SelectInputQueryContext?> CreateInputContextAsync(
        FunctionCallNode functionCallNode,
        FunctionCallNode? formatNode,
        string alias,
        CancellationToken cancellationToken)
    {
        var @delegate = await _createDelegateVisitor.RunAndReturnAsync(functionCallNode, cancellationToken);
        var source = await @delegate.InvokeAsync(_executionThread, cancellationToken);
        if (@delegate is FunctionCallFuncUnitDecorator callFuncUnitDecorator)
        {
            callFuncUnitDecorator.CacheEnabled = false;
        }
        return await _rowsInputFactory.CreateRowsInputAsync(
            source, alias, _executionThread, formatNode, true, cancellationToken);
    }

    private async ValueTask<SelectInputQueryContext?> CreateVaryInputContextAsync(
        FunctionCallNode functionCallNode,
        FunctionCallNode? formatNode,
        string alias,
        CancellationToken cancellationToken)
    {
        var @delegate = await _createDelegateVisitor.RunAndReturnAsync(functionCallNode, cancellationToken);
        var store = new FunctionResultStore(
            @delegate,
            functionCallNode.GetRequiredAttribute<FuncUnitCallInfo>(AstAttributeKeys.ArgumentsKey));
        var source = (await store.CallAsync(_executionThread, cancellationToken)).Value;
        var context = await _rowsInputFactory.CreateRowsInputAsync(
            source, alias, _executionThread, formatNode, true, cancellationToken);
        if (context == null)
        {
            return null;
        }
        var varyingRowsInput = new VaryingRowsInput(_executionThread, context.RowsInput, store, _rowsInputFactory,
            context);
        context.RowsInput = varyingRowsInput;
        if (@delegate is FunctionCallFuncUnitDecorator callFuncUnitDecorator)
        {
            callFuncUnitDecorator.CacheEnabled = false;
        }
        return context;
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
