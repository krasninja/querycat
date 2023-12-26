using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational;

internal sealed class FuncUnitRowsIteratorColumn(IRowsIterator rowsIterator, int columnIndex) : IFuncUnit
{
    /// <inheritdoc />
    public DataType OutputType => rowsIterator.Columns[columnIndex].DataType;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public VariantValue Invoke() => rowsIterator.Current[columnIndex];

    /// <inheritdoc />
    public override string ToString() => $"{nameof(FuncUnitRowsIteratorColumn)}: {rowsIterator}, {columnIndex}";
}
