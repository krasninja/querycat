using System.Collections;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Inputs;

/// <summary>
/// The class is used to create an instance of <see cref="IRowsInput" /> from <see cref="VariantValue" />.
/// </summary>
internal sealed class RowsInputFactory
{
    private readonly SelectCommandContext _context;

    public RowsInputFactory(SelectCommandContext context)
    {
        _context = context;
    }

    public ValueTask<SelectInputQueryContext?> CreateRowsInputAsync(
        VariantValue source,
        IExecutionThread<ExecutionOptions> executionThread,
        bool resolveStringAsSource = false,
        CancellationToken cancellationToken = default)
        => CreateRowsInputAsync(source, string.Empty, executionThread, null, resolveStringAsSource, cancellationToken);

    public async ValueTask<SelectInputQueryContext?> CreateRowsInputAsync(
        VariantValue source,
        string alias,
        IExecutionThread<ExecutionOptions> executionThread,
        FunctionCallNode? formatNode,
        bool resolveStringAsSource = false,
        CancellationToken cancellationToken = default)
    {
        if (resolveStringAsSource && source.Type == DataType.String)
        {
            var createDelegateVisitor = new SelectCreateDelegateVisitor(executionThread, _context);
            var stringRowsInputContext = await CreateInputSourceFromStringVariableAsync(
                source.AsStringUnsafe,
                executionThread,
                createDelegateVisitor,
                formatNode,
                cancellationToken);
            return stringRowsInputContext;
        }
        if (DataTypeUtils.IsSimple(source.Type))
        {
            var singleValueRowsInput = new SingleValueRowsInput(source);
            var context = new SelectInputQueryContext(singleValueRowsInput);
            singleValueRowsInput.QueryContext = context;
            return context;
        }
        if (source.Type == DataType.Object)
        {
            if (source.AsObjectUnsafe is IRowsInput rowsInput)
            {
                SelectInputQueryContext queryContext;
                if (rowsInput.QueryContext is not SelectInputQueryContext selectInputQueryContext)
                {
                    var targetColumns = await _context.GetSelectIdentifierColumnsAsync(alias, cancellationToken);
                    queryContext = new SelectInputQueryContext(rowsInput, targetColumns, executionThread.ConfigStorage);
                    if (_context.Parent != null && !executionThread.Options.DisableCache)
                    {
                        rowsInput = new CacheRowsInput(executionThread, rowsInput, _context.Conditions);
                    }
                }
                else
                {
                    queryContext = selectInputQueryContext;
                }
                rowsInput.QueryContext = queryContext;
                return queryContext;
            }
            if (source.AsObjectUnsafe is IRowsIterator rowsIterator)
            {
                rowsInput = new RowsIteratorInput(rowsIterator);
                var context = new SelectInputQueryContext(rowsInput);
                rowsInput.QueryContext = context;
                return context;
            }
            if (source.AsObjectUnsafe is IEnumerable enumerable && enumerable.GetType().IsGenericType)
            {
#pragma warning disable IL2072
                rowsInput = new CollectionInput(TypeUtils.GetUnderlyingType(enumerable), enumerable);
                var context = new SelectInputQueryContext(rowsInput);
                rowsInput.QueryContext = context;
#pragma warning restore IL2072
                return context;
            }
        }

        return null;
    }

    private static async Task<SelectInputQueryContext> CreateInputSourceFromStringVariableAsync(
        string strVariable,
        IExecutionThread executionThread,
        CreateDelegateVisitor createDelegateVisitor,
        FunctionCallNode? formatNode,
        CancellationToken cancellationToken)
    {
        var args = new FunctionCallArguments()
            .Add("uri", new VariantValue(strVariable));
        if (formatNode != null)
        {
            var @delegate = await createDelegateVisitor.RunAndReturnAsync(formatNode, cancellationToken);
            var formatter = await @delegate.InvokeAsync(executionThread, cancellationToken);
            args.Add("fmt", formatter);
        }
        var rowsInput = (await executionThread.FunctionsManager.CallFunctionAsync("read", executionThread, args, cancellationToken))
            .AsRequired<IRowsInput>();
        var context = new SelectInputQueryContext(rowsInput, rowsInput.Columns, executionThread.ConfigStorage);
        rowsInput.QueryContext = context;
        return context;
    }
}
