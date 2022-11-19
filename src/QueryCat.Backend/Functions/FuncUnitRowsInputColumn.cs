using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

internal sealed class FuncUnitRowsInputColumn : FuncUnit
{
    private readonly IRowsInput _rowsInput;
    private readonly int _columnIndex;

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
