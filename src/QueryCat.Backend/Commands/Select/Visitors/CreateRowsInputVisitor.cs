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
    private readonly ResolveTypesVisitor _resolveTypesVisitor;

    public CreateRowsInputVisitor(ExecutionThread executionThread)
    {
        _executionThread = executionThread;
        _resolveTypesVisitor = new ResolveTypesVisitor(executionThread);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableFunctionNode node)
    {
        var queryNode = AstTraversal.GetFirstParent<SelectQueryNode>();
        if (queryNode == null)
        {
            throw new InvalidOperationException(
                $"{nameof(SelectTableFunctionNode)} does not have root query node. Invalid AST tree.");
        }
        var context = queryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);

        // Determine types for the node.
        var typesVisitor = _resolveTypesVisitor;
        if (context.Parent != null)
        {
            typesVisitor = new SelectResolveTypesVisitor(_executionThread, context.Parent);
        }
        typesVisitor.Run(node.TableFunction);

        var source = new CreateDelegateVisitor(_executionThread).RunAndReturn(node.TableFunction).Invoke();
        var rowsInput = CreateRowsInput(context, source);
        FixInputColumnTypes(rowsInput);
        SetAlias(rowsInput, node.Alias);
        node.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);
    }

    private IRowsInput CreateRowsInput(SelectCommandContext context, VariantValue source)
    {
        if (DataTypeUtils.IsSimple(source.GetInternalType()))
        {
            return new SingleValueRowsInput(source);
        }
        if (source.AsObject is IRowsInput rowsInput)
        {
            var queryContext = new SelectInputQueryContext(rowsInput)
            {
                InputConfigStorage = _executionThread.InputConfigStorage
            };
            context.InputQueryContextList.Add(queryContext);
            if (context.Parent != null)
            {
                rowsInput = new CacheRowsInput(rowsInput);
            }
            rowsInput.SetContext(queryContext);
            rowsInput.Open();
            Log.Logger.Debug("Open rows input {RowsInput}.", rowsInput);
            return rowsInput;
        }
        if (source.AsObject is IRowsIterator rowsIterator)
        {
            return new RowsIteratorInput(rowsIterator);
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
