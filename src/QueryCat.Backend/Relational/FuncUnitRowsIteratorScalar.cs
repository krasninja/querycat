using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational;

internal sealed class FuncUnitRowsIteratorScalar(IRowsIterator rowsIterator, int columnIndex = 0) : IFuncUnit
{
    private bool _isExecuted;
    private VariantValue _value = VariantValue.Null;

    /// <inheritdoc />
    public DataType OutputType => rowsIterator.Columns[columnIndex].DataType;

    /// <inheritdoc />
    public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        if (!_isExecuted)
        {
            var hasData = await rowsIterator.MoveNextAsync(cancellationToken);
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
