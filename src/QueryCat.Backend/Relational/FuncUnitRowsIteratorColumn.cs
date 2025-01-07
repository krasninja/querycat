using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational;

internal sealed class FuncUnitRowsIteratorColumn(IRowsIterator rowsIterator, int columnIndex) : IFuncUnit
{
    /// <inheritdoc />
    public DataType OutputType => rowsIterator.Columns[columnIndex].DataType;

    /// <inheritdoc />
    public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(rowsIterator.Current[columnIndex]);

    /// <inheritdoc />
    public override string ToString() => $"{nameof(FuncUnitRowsIteratorColumn)}: {rowsIterator}, {columnIndex}";
}
