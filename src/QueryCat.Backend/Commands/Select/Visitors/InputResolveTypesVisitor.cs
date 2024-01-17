using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal class InputResolveTypesVisitor : ResolveTypesVisitor
{
    private readonly IRowsInput[] _rowsInputs;

    /// <inheritdoc />
    public InputResolveTypesVisitor(IExecutionThread<ExecutionOptions> executionThread, IRowsInput leftInput,
        IRowsInput rightInput)
        : base(executionThread)
    {
        _rowsInputs = [leftInput, rightInput];
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
                node.SetDataType(rowsInput.Columns[columnIndex].DataType);
                return;
            }
        }

        base.Visit(node);
    }
}
