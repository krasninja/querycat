using System.Runtime.CompilerServices;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

internal sealed class FuncUnitFromRowsIterator : FuncUnit
{
    private readonly IRowsIterator _rowsIterator;
    private readonly int _columnIndex;

    public FuncUnitFromRowsIterator(IRowsIterator rowsIterator, int columnIndex)
    {
        _rowsIterator = rowsIterator;
        _columnIndex = columnIndex;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override VariantValue Invoke() => _rowsIterator.Current[_columnIndex];

    /// <inheritdoc />
    public override string ToString() => $"{nameof(FuncUnitFromRowsIterator)}: {_rowsIterator}, {_columnIndex}";
}
