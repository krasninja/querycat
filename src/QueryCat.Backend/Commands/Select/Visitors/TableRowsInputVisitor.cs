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
internal sealed class TableRowsInputVisitor : AstVisitor
{
    private readonly ExecutionThread _executionThread;
    private readonly ResolveTypesVisitor _resolveTypesVisitor;
    private SelectQuerySpecificationNode? _rootQueryNode;

    public TableRowsInputVisitor(ExecutionThread executionThread)
    {
        _executionThread = executionThread;
        _resolveTypesVisitor = new ResolveTypesVisitor(executionThread);
    }

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node)
    {
        if (_rootQueryNode == null)
        {
            _rootQueryNode = node;
        }
    }

    /// <inheritdoc />
    public override void Visit(SelectTableFunctionNode node)
    {
        _resolveTypesVisitor.Run(node.TableFunction);
        var source = new CreateDelegateVisitor(_executionThread)
            .RunAndReturn(node.TableFunction).Invoke();
        var rowsInput = CreateRowsInput(source, IsSubQuery());
        FixInputColumnTypes(rowsInput);
        SetAlias(rowsInput, node.Alias);
        node.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);
    }

    private bool IsSubQuery()
    {
        var queryNode = AstTraversal.GetFirstParent<SelectQuerySpecificationNode>();
        return queryNode != _rootQueryNode;
    }

    private IRowsInput CreateRowsInput(VariantValue source, bool isSubQuery)
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
            if (isSubQuery)
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
