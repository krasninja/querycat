using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

internal sealed class FuncUnitRowsInputColumn(IRowsInput rowsInput, int columnIndex) : IFuncUnit
{
    /// <inheritdoc />
    public DataType OutputType => rowsInput.Columns[columnIndex].DataType;

    /// <inheritdoc />
    public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        rowsInput.ReadValue(columnIndex, out VariantValue value);
        return ValueTask.FromResult(value);
    }
}
