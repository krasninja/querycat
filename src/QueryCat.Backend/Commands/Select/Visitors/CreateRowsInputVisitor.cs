using Microsoft.Extensions.Logging;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Inputs;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// Create <see cref="IRowsInput" /> instances for every FROM clause. It will call the function and opens rows input.
/// So after that visitor all node types will be set. The <see cref="IRowsInput" /> input will be put into
/// <see cref="SelectTableFunctionNode" /> node with AstAttributeKeys.RowsInputKey key.
/// </summary>
internal sealed class CreateRowsInputVisitor : AstVisitor
{
    private readonly ExecutionThread _executionThread;
    private readonly SelectCommandContext _context;
    private readonly ResolveTypesVisitor _resolveTypesVisitor;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<CreateRowsInputVisitor>();

    public CreateRowsInputVisitor(ExecutionThread executionThread, SelectCommandContext context)
    {
        _executionThread = executionThread;
        _context = context;
        _resolveTypesVisitor = new ResolveTypesVisitor(executionThread);
        AstTraversal.TypesToIgnore.Add(typeof(SelectQueryNode));
        AstTraversal.AcceptBeforeIgnore = true;
    }

    /// <inheritdoc />
    public override void Visit(SelectTableFunctionNode node)
    {
        // Determine types for the node.
        var typesVisitor = _resolveTypesVisitor;
        if (_context.Parent != null)
        {
            typesVisitor = new SelectResolveTypesVisitor(_executionThread, _context.Parent);
        }
        typesVisitor.Run(node.TableFunctionNode);

        var source = new CreateDelegateVisitor(_executionThread).RunAndReturn(node.TableFunctionNode).Invoke();
        var inputContext = CreateRowsInput(source);
        inputContext.Alias = node.Alias;
        _context.AddInput(inputContext);

        SetAlias(inputContext.RowsInput, node.Alias);

        node.SetAttribute(AstAttributeKeys.RowsInputKey, inputContext.RowsInput);
    }

    private SelectCommandInputContext CreateRowsInput(VariantValue source)
    {
        if (DataTypeUtils.IsSimple(source.GetInternalType()))
        {
            return new SelectCommandInputContext(new SingleValueRowsInput(source));
        }
        if (source.AsObject is IRowsInput rowsInput)
        {
            var queryContext = new SelectInputQueryContext(rowsInput)
            {
                InputConfigStorage = _executionThread.ConfigStorage,
            };
            if (_context.Parent != null && !_executionThread.Options.DisableCache)
            {
                rowsInput = new CacheRowsInput(rowsInput, _context.Conditions);
            }
            rowsInput.QueryContext = queryContext;
            rowsInput.Open();
            _logger.LogDebug("Open rows input {RowsInput}.", rowsInput);
            return new SelectCommandInputContext(rowsInput, queryContext);
        }
        if (source.AsObject is IRowsIterator rowsIterator)
        {
            return new SelectCommandInputContext(new RowsIteratorInput(rowsIterator));
        }

        throw new QueryCatException("Invalid rows input.");
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
