using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.FunctionsManager;

internal sealed class FuncUnitRowsIteratorScalar : FuncUnit
{
    private readonly IRowsIterator _rowsIterator;
    private readonly int _columnIndex;
    private bool _isExecuted;
    private VariantValue _value = VariantValue.Null;

    /// <inheritdoc />
    public override DataType OutputType => _rowsIterator.Columns[_columnIndex].DataType;

    public FuncUnitRowsIteratorScalar(IRowsIterator rowsIterator, int columnIndex = 0)
    {
        _rowsIterator = rowsIterator;
        _columnIndex = columnIndex;
    }

    /// <inheritdoc />
    public override VariantValue Invoke()
    {
        if (!_isExecuted)
        {
            var hasData = _rowsIterator.MoveNext();
            if (hasData)
            {
                _value = _rowsIterator.Current[_columnIndex];
            }
            _isExecuted = true;
        }
        return _value;
    }

    /// <inheritdoc />
    public override string ToString() => $"{nameof(FuncUnitRowsIteratorScalar)}: {_rowsIterator}, {_columnIndex}";
}
