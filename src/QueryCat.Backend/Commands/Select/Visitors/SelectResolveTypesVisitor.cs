using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed class SelectResolveTypesVisitor : ResolveTypesVisitor
{
    private readonly SelectCommandContext _selectCommandContext;

    /// <inheritdoc />
    public SelectResolveTypesVisitor(ExecutionThread executionThread, SelectCommandContext selectCommandContext) :
        base(executionThread)
    {
        _selectCommandContext = selectCommandContext;
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        var columnIndex = _selectCommandContext.GetColumnIndexByName(node.Name, node.SourceName, out SelectCommandContext? commandContext);
        if (columnIndex < 0)
        {
            base.Visit(node);
        }
        else
        {
            node.SetAttribute(AstAttributeKeys.InputColumn, commandContext!.CurrentIterator.Columns[columnIndex]);
            node.SetDataType(commandContext.CurrentIterator.Columns[columnIndex].DataType);
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
        var columnIndex = _selectCommandContext.GetColumnIndexByName(node.ColumnName, node.SourceName, out SelectCommandContext? commandContext);
        if (columnIndex < 0)
        {
            base.Visit(node);
        }
        else
        {
            node.SetAttribute(AstAttributeKeys.InputColumn, commandContext!.CurrentIterator.Columns[columnIndex]);
            node.SetDataType(commandContext.CurrentIterator.Columns[columnIndex].DataType);
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
}
