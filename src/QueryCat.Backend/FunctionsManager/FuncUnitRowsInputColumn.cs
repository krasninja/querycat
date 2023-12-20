using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.FunctionsManager;

internal sealed class FuncUnitRowsInputColumn : FuncUnit
{
    private readonly IRowsInput _rowsInput;
    private readonly int _columnIndex;

    /// <inheritdoc />
    public override DataType OutputType => _rowsInput.Columns[_columnIndex].DataType;

    public FuncUnitRowsInputColumn(IRowsInput rowsInput, int columnIndex)
    {
        _rowsInput = rowsInput;
        _columnIndex = columnIndex;
    }

    /// <inheritdoc />
    public override VariantValue Invoke()
    {
        _rowsInput.ReadValue(_columnIndex, out VariantValue value);
        return value;
    }
}
