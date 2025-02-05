using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Inputs;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
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

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(CreateRowsInputVisitor));

    public CreateRowsInputVisitor(IExecutionThread<ExecutionOptions> executionThread, SelectCommandContext context)
    {
        _executionThread = executionThread;
        _context = context;
        _resolveTypesVisitor = new ResolveTypesVisitor(executionThread);
        AstTraversal.TypesToIgnore.Add(typeof(SelectQueryNode));
        AstTraversal.AcceptBeforeIgnore = true;
    }

    /// <inheritdoc />
    public override async ValueTask VisitAsync(SelectTableFunctionNode node, CancellationToken cancellationToken)
    {
        // Determine types for the node.
        var typesVisitor = _resolveTypesVisitor;
        if (_context.Parent != null)
        {
            typesVisitor = new SelectResolveTypesVisitor(_executionThread, _context.Parent);
        }
        await typesVisitor.RunAsync(node.TableFunctionNode, cancellationToken);

        var @delegate = await new CreateDelegateVisitor(_executionThread, typesVisitor)
            .RunAndReturnAsync(node.TableFunctionNode, cancellationToken);
        var source = await @delegate.InvokeAsync(_executionThread, cancellationToken);
        var inputContext = await CreateRowsInputAsync(source, node.Alias, cancellationToken);
        inputContext.Alias = node.Alias;
        _context.AddInput(inputContext);

        SetAlias(inputContext.RowsInput, node.Alias);

        node.SetAttribute(AstAttributeKeys.RowsInputKey, inputContext.RowsInput);
    }

    private async Task<SelectCommandInputContext> CreateRowsInputAsync(VariantValue source, string alias, CancellationToken cancellationToken)
    {
        if (DataTypeUtils.IsSimple(source.Type))
        {
            return new SelectCommandInputContext(new SingleValueRowsInput(source));
        }
        if (source.AsObject is IRowsInput rowsInput)
        {
            var targetColumns = _context.GetSelectIdentifierColumns(alias);
            var queryContext = new SelectInputQueryContext(rowsInput, targetColumns)
            {
                InputConfigStorage = _executionThread.ConfigStorage,
            };
            if (_context.Parent != null && !_executionThread.Options.DisableCache)
            {
                rowsInput = new CacheRowsInput(_executionThread, rowsInput, _context.Conditions);
            }
            rowsInput.QueryContext = queryContext;
            await rowsInput.OpenAsync(cancellationToken);
            _logger.LogDebug("Open rows input {RowsInput}.", rowsInput);
            return new SelectCommandInputContext(rowsInput, queryContext);
        }
        if (source.AsObject is IRowsIterator rowsIterator)
        {
            return new SelectCommandInputContext(new RowsIteratorInput(rowsIterator));
        }

        throw new QueryCatException(Resources.Errors.InvalidRowsInput);
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
}
