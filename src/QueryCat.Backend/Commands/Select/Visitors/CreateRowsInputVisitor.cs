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
using QueryCat.Backend.Core.Functions;
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
    private readonly CreateDelegateVisitor _createDelegateVisitor;

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
        _createDelegateVisitor = new CreateDelegateVisitorWithValueStore(executionThread, _resolveTypesVisitor);
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
            rowsInput = await CreateInputSourceFromStringVariableAsync(value.AsStringUnsafe,
                node.Format, cancellationToken);
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

        var @delegate = await _createDelegateVisitor.RunAndReturnAsync(node, cancellationToken);
        var source = await @delegate.InvokeAsync(_executionThread, cancellationToken);
        var inputContext = await CreateRowsInputAsync(source, alias, formatNode, cancellationToken);
        if (inputContext == null)
        {
            return null;
        }
        inputContext.Alias = alias;
        SetAlias(inputContext.RowsInput, alias);
        _context.AddInput(inputContext);

        return inputContext.RowsInput;
    }

    private async ValueTask<SelectCommandInputContext?> CreateRowsInputAsync(
        VariantValue source,
        string alias,
        FunctionCallNode? formatNode,
        CancellationToken cancellationToken)
    {
        if (source.Type == DataType.String)
        {
            var stringInput = await CreateInputSourceFromStringVariableAsync(source.AsStringUnsafe,
                formatNode, cancellationToken);
            return new SelectCommandInputContext(stringInput);
        }
        if (DataTypeUtils.IsSimple(source.Type))
        {
            return new SelectCommandInputContext(new SingleValueRowsInput(source));
        }
        if (source.AsObject is IRowsInput rowsInput)
        {
            if (rowsInput.QueryContext is not SelectInputQueryContext queryContext)
            {
                var targetColumns = await _context.GetSelectIdentifierColumnsAsync(alias, cancellationToken);
                queryContext = new SelectInputQueryContext(rowsInput, targetColumns)
                {
                    InputConfigStorage = _executionThread.ConfigStorage,
                };
                if (_context.Parent != null && !_executionThread.Options.DisableCache)
                {
                    rowsInput = new CacheRowsInput(_executionThread, rowsInput, _context.Conditions);
                }
                rowsInput.QueryContext = queryContext;
                await OpenRowsInput(rowsInput, cancellationToken);
            }
            return new SelectCommandInputContext(rowsInput, queryContext);
        }
        if (source.AsObject is IRowsIterator rowsIterator)
        {
            return new SelectCommandInputContext(new RowsIteratorInput(rowsIterator));
        }

        return null;
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
            await OpenRowsInput(rowsInput, cancellationToken);
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

    private async Task<IRowsInput> CreateInputSourceFromStringVariableAsync(
        string strVariable,
        FunctionCallNode? formatterNode,
        CancellationToken cancellationToken)
    {
        var args = new FunctionCallArguments()
            .Add("uri", new VariantValue(strVariable));
        if (formatterNode != null)
        {
            var @delegate = await _createDelegateVisitor.RunAndReturnAsync(formatterNode, cancellationToken);
            var formatter = await @delegate.InvokeAsync(_executionThread, cancellationToken);
            args.Add("fmt", formatter);
        }
        var rowsInput = (await _executionThread.FunctionsManager.CallFunctionAsync("read", _executionThread, args, cancellationToken))
            .AsRequired<IRowsInput>();
        rowsInput.QueryContext = new SelectInputQueryContext(rowsInput);
        await OpenRowsInput(rowsInput, cancellationToken);
        return rowsInput;
    }

    private async ValueTask OpenRowsInput(IRowsInput rowsInput, CancellationToken cancellationToken)
    {
        await rowsInput.OpenAsync(cancellationToken);
        _logger.LogDebug("Open rows input {RowsInput}.", rowsInput);
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
    private sealed class CreateDelegateVisitorWithValueStore : CreateDelegateVisitor
    {
        /// <summary>
        /// NodeId -> Object map.
        /// </summary>
        private readonly Dictionary<int, VariantValue> _nodeIdFuncMap = new();

        private sealed class FunctionCallFuncUnitFacade(
            IFuncUnit func,
            int nodeId,
            IDictionary<int, VariantValue> cacheMap) : IFuncUnit
        {
            /// <inheritdoc />
            public DataType OutputType => func.OutputType;

            /// <inheritdoc />
            public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
            {
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

        /// <inheritdoc />
        public CreateDelegateVisitorWithValueStore(IExecutionThread<ExecutionOptions> thread, ResolveTypesVisitor resolveTypesVisitor)
            : base(thread, resolveTypesVisitor)
        {
        }

        /// <inheritdoc />
        public override async ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
        {
            await base.VisitAsync(node, cancellationToken);
            var func = NodeIdFuncMap[node.Id];
            NodeIdFuncMap[node.Id] = new FunctionCallFuncUnitFacade(func, node.Id, _nodeIdFuncMap);
        }
    }
}
