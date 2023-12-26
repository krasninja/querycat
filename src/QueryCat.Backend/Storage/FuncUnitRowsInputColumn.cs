using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

internal sealed class FuncUnitRowsInputColumn(IRowsInput rowsInput, int columnIndex) : IFuncUnit
{
    /// <inheritdoc />
    public DataType OutputType => rowsInput.Columns[columnIndex].DataType;

    /// <inheritdoc />
    public VariantValue Invoke()
    {
        rowsInput.ReadValue(columnIndex, out VariantValue value);
        return value;
    }
}
