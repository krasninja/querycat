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
    public override ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        foreach (var rowsInput in _rowsInputs)
        {
            var columnIndex = rowsInput.GetColumnIndexByName(node.TableFieldName, node.TableSourceName);
            if (columnIndex > -1)
            {
                node.SetAttribute(AstAttributeKeys.InputColumnKey, rowsInput.Columns[columnIndex]);
                node.SetDataType(rowsInput.Columns[columnIndex].DataType);
                return ValueTask.CompletedTask;
            }
        }

        return base.VisitAsync(node, cancellationToken);
    }
}
