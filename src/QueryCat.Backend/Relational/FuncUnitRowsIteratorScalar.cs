using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational;

internal sealed class FuncUnitRowsIteratorScalar(IRowsIterator rowsIterator, int columnIndex = 0) : IFuncUnit
{
    private bool _isExecuted;
    private VariantValue _value = VariantValue.Null;

    /// <inheritdoc />
    public DataType OutputType => rowsIterator.Columns[columnIndex].DataType;

    /// <inheritdoc />
    public VariantValue Invoke()
    {
        if (!_isExecuted)
        {
            var hasData = rowsIterator.MoveNext();
            if (hasData)
            {
                _value = rowsIterator.Current[columnIndex];
            }
            _isExecuted = true;
        }
        return _value;
    }

    /// <inheritdoc />
    public override string ToString() => $"{nameof(FuncUnitRowsIteratorScalar)}: {rowsIterator}, {columnIndex}";
}
