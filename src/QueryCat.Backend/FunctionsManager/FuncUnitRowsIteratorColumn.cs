using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.FunctionsManager;

internal sealed class FuncUnitRowsIteratorColumn : FuncUnit
{
    private readonly IRowsIterator _rowsIterator;
    private readonly int _columnIndex;

    /// <inheritdoc />
    public override DataType OutputType => _rowsIterator.Columns[_columnIndex].DataType;

    public FuncUnitRowsIteratorColumn(IRowsIterator rowsIterator, int columnIndex)
    {
        _rowsIterator = rowsIterator;
        _columnIndex = columnIndex;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override VariantValue Invoke() => _rowsIterator.Current[_columnIndex];

    /// <inheritdoc />
    public override string ToString() => $"{nameof(FuncUnitRowsIteratorColumn)}: {_rowsIterator}, {_columnIndex}";
}
