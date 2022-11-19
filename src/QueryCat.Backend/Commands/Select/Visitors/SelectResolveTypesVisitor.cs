using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed class SelectResolveTypesVisitor : ResolveTypesVisitor
{
    private readonly SelectCommandContext _context;

    /// <inheritdoc />
    public SelectResolveTypesVisitor(ExecutionThread executionThread, SelectCommandContext context) :
        base(executionThread)
    {
        _context = context;
        AstTraversal.TypesToIgnore.Add(typeof(SelectQuerySpecificationNode));
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        var columnIndex = _context
            .GetColumnIndexByName(node.Name, node.SourceName, out var rowsIterator);
        if (columnIndex < 0)
        {
            base.Visit(node);
        }
        else
        {
            node.SetAttribute(AstAttributeKeys.InputColumnKey, rowsIterator!.Columns[columnIndex]);
            node.SetAttribute(AstAttributeKeys.InputColumnIndexKey, columnIndex);
            node.SetDataType(rowsIterator.Columns[columnIndex].DataType);
        }
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistExpressionNode node)
    {
        node.ExpressionNode.CopyTo<DataType>(AstAttributeKeys.TypeKey, node);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistNameNode node)
    {
        var columnIndex = _context
            .GetColumnIndexByName(node.ColumnName, node.SourceName, out var rowsIterator);
        if (columnIndex < 0)
        {
            base.Visit(node);
        }
        else
        {
            node.SetAttribute(AstAttributeKeys.InputColumnKey, rowsIterator!.Columns[columnIndex]);
            node.SetDataType(rowsIterator.Columns[columnIndex].DataType);
        }
    }

    /// <inheritdoc />
    public override void Visit(SelectQueryExpressionBodyNode node)
    {
        node.SetDataType(node.Queries[0].ColumnsList.Columns[0].GetDataType());
    }

    /// <inheritdoc />
    public override void Visit(SelectExistsExpressionNode node)
    {
        node.SetDataType(DataType.Boolean);
    }

    /// <inheritdoc />
    public override void Visit(SelectOrderBySpecificationNode node)
    {
        node.SetDataType(node.Expression.GetDataType());
    }

    /// <inheritdoc />
    public override void Visit(SelectSubqueryConditionExpressionNode node)
    {
        node.SetDataType(DataType.Boolean);
    }
}
