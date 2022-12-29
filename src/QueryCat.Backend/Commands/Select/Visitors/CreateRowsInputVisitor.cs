using Serilog;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Relational;
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
        typesVisitor.Run(node.TableFunction);

        var source = new CreateDelegateVisitor(_executionThread).RunAndReturn(node.TableFunction).Invoke();
        var inputContext = CreateRowsInput(source);
        inputContext.Alias = node.Alias;
        _context.Inputs.Add(inputContext);

        FixInputColumnTypes(inputContext.RowsInput);
        SetAlias(inputContext.RowsInput, node.Alias);

        node.SetAttribute(AstAttributeKeys.RowsInputKey, inputContext.RowsInput);
    }

    private SelectCommandContextInput CreateRowsInput(VariantValue source)
    {
        if (DataTypeUtils.IsSimple(source.GetInternalType()))
        {
            return new SelectCommandContextInput(new SingleValueRowsInput(source));
        }
        if (source.AsObject is IRowsInput rowsInput)
        {
            var queryContext = new SelectInputQueryContext(rowsInput)
            {
                InputConfigStorage = _executionThread.InputConfigStorage
            };
            if (_context.Parent != null)
            {
                rowsInput = new CacheRowsInput(rowsInput);
            }
            rowsInput.SetContext(queryContext);
            rowsInput.Open();
            Log.Logger.Debug("Open rows input {RowsInput}.", rowsInput);
            return new SelectCommandContextInput(rowsInput, queryContext);
        }
        if (source.AsObject is IRowsIterator rowsIterator)
        {
            return new SelectCommandContextInput(new RowsIteratorInput(rowsIterator));
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

    /// <summary>
    /// Find the expressions in SELECT output area like CAST(id AS string).
    /// </summary>
    private void FixInputColumnTypes(IRowsInput rowsInput)
    {
        var querySpecificationNodes = AstTraversal.GetParents<SelectQuerySpecificationNode>().ToList();
        foreach (var querySpecificationNode in querySpecificationNodes)
        {
            foreach (var castNode in querySpecificationNode.ColumnsListNode.GetAllChildren<CastFunctionNode>())
            {
                if (castNode.ExpressionNode is not IdentifierExpressionNode idNode)
                {
                    continue;
                }

                var columnIndex = rowsInput.GetColumnIndexByName(idNode.Name, idNode.SourceName);
                if (columnIndex > -1)
                {
                    rowsInput.Columns[columnIndex].DataType = castNode.TargetTypeNode.Type;
                }
            }
        }
    }
}
