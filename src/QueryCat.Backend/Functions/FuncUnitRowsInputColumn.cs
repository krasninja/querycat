using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

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
