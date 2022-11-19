using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Inputs;

internal class SelectInputCreateDelegateVisitor : CreateDelegateVisitor
{
    private readonly IRowsInput[] _rowsInputs;

    /// <inheritdoc />
    public SelectInputCreateDelegateVisitor(ExecutionThread thread, params IRowsInput[] rowsInputs) : base(thread)
    {
        _rowsInputs = rowsInputs;
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        var rowsInput = node.GetAttribute<IRowsInput>(AstAttributeKeys.RowsInputKey);
        if (rowsInput == null)
        {
            throw new InvalidOperationException("Rows input not initialized.");
        }
        var columnIndex = node.GetAttribute<int>(AstAttributeKeys.InputColumnIndexKey);
        NodeIdFuncMap[node.Id] = new FuncUnitRowsInputColumn(rowsInput, columnIndex);
    }
}
