using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Inputs;

internal class InputResolveTypesVisitor : ResolveTypesVisitor
{
    private readonly IRowsInput[] _rowsInputs;

    /// <inheritdoc />
    public InputResolveTypesVisitor(ExecutionThread executionThread, params IRowsInput[] rowsInputs)
        : base(executionThread)
    {
        _rowsInputs = rowsInputs;
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        foreach (var rowsInput in _rowsInputs)
        {
            var columnIndex = rowsInput
                .GetColumnIndexByName(node.Name, node.SourceName);
            if (columnIndex > -1)
            {
                node.SetAttribute(AstAttributeKeys.InputColumnKey, rowsInput.Columns[columnIndex]);
                node.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);
                node.SetAttribute(AstAttributeKeys.InputColumnIndexKey, columnIndex);
                node.SetDataType(rowsInput.Columns[columnIndex].DataType);
                return;
            }
        }

        base.Visit(node);
    }
}
