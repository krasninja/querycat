using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed class SelectResolveTypesVisitor : ResolveTypesVisitor
{
    private readonly IRowsIterator _rowsIterator;

    /// <inheritdoc />
    public SelectResolveTypesVisitor(ExecutionThread executionThread, IRowsIterator rowsIterator) :
        base(executionThread)
    {
        _rowsIterator = rowsIterator;
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        var columnIndex = _rowsIterator.GetColumnIndexByName(node.Name, node.SourceName);
        if (columnIndex < 0)
        {
            base.Visit(node);
        }
        else
        {
            node.SetAttribute(AstAttributeKeys.InputColumn, _rowsIterator.Columns[columnIndex]);
            node.SetDataType(_rowsIterator.Columns[columnIndex].DataType);
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
        var columnIndex = _rowsIterator.GetColumnIndexByName(node.ColumnName, node.SourceName);
        if (columnIndex < 0)
        {
            base.Visit(node);
        }
        else
        {
            node.SetDataType(_rowsIterator.Columns[columnIndex].DataType);
        }
    }

    /// <inheritdoc />
    public override void Visit(SelectQueryExpressionBodyNode node)
    {
        node.SetDataType(node.Queries[0].ColumnsList.Columns[0].GetDataType());
    }
}
