using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Core;
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
    private readonly string _alias;
    private readonly FunctionCallNode? _formatNode;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(RowsInputFactory));

    public RowsInputFactory(
        SelectCommandContext context,
        string alias,
        FunctionCallNode? formatNode)
    {
        _context = context;
        _alias = alias;
        _formatNode = formatNode;
    }

    public async ValueTask<SelectCommandInputContext?> CreateRowsInputAsync(
        VariantValue source,
        IExecutionThread<ExecutionOptions> executionThread,
        CancellationToken cancellationToken)
    {
        var createDelegateVisitor = new SelectCreateDelegateVisitor(executionThread, _context);

        if (source.Type == DataType.String)
        {
            var stringRowsInput = await CreateInputSourceFromStringVariableAsync(
                source.AsStringUnsafe,
                executionThread,
                createDelegateVisitor,
                _formatNode,
                cancellationToken);
            _logger.LogDebug("Open rows input {RowsInput} from string.", stringRowsInput);
            return new SelectCommandInputContext(stringRowsInput);
        }
        if (DataTypeUtils.IsSimple(source.Type))
        {
            var singleValueRowsInput = new SingleValueRowsInput(source);
            _logger.LogDebug("Open rows input {RowsInput} from simple type.", singleValueRowsInput);
            return new SelectCommandInputContext(singleValueRowsInput);
        }
        if (source.AsObject is IRowsInput rowsInput)
        {
            if (rowsInput.QueryContext is not SelectInputQueryContext queryContext)
            {
                var targetColumns = await _context.GetSelectIdentifierColumnsAsync(_alias, cancellationToken);
                queryContext = new SelectInputQueryContext(rowsInput, targetColumns, executionThread.ConfigStorage);
                if (_context.Parent != null && !executionThread.Options.DisableCache)
                {
                    rowsInput = new CacheRowsInput(executionThread, rowsInput, _context.Conditions);
                }
                rowsInput.QueryContext = queryContext;
                await rowsInput.OpenAsync(cancellationToken);
            }
            _logger.LogDebug("Open rows input {RowsInput} from object.", rowsInput);
            return new SelectCommandInputContext(rowsInput, queryContext);
        }
        if (source.AsObject is IRowsIterator rowsIterator)
        {
            rowsInput = new RowsIteratorInput(rowsIterator);
            _logger.LogDebug("Open rows input {RowsInput} from iterator.", rowsInput);
            return new SelectCommandInputContext(rowsInput);
        }

        return null;
    }

    public static async Task<IRowsInput> CreateInputSourceFromStringVariableAsync(
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
        rowsInput.QueryContext = new SelectInputQueryContext(rowsInput, rowsInput.Columns, executionThread.ConfigStorage);
        await rowsInput.OpenAsync(cancellationToken);
        return rowsInput;
    }
}
