using QueryCat.Backend.Core;
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
        TryInvokeAsync(thread, out var value, cancellationToken);
        return ValueTask.FromResult(value);
    }

    internal ValueTask<bool> TryInvokeAsync(IExecutionThread thread, out VariantValue value, CancellationToken cancellationToken = default)
    {
        var errorCode = rowsInput.ReadValue(columnIndex, out value);
        return ValueTask.FromResult(errorCode == ErrorCode.OK);
    }
}
