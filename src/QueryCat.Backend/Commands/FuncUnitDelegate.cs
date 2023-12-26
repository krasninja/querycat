using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncUnitDelegate(Func<VariantValue> func, DataType outputType) : IFuncUnit
{
    public IReadOnlyList<IRowsIterator>? SubQueryIterators { get; set; }

    /// <inheritdoc />
    public DataType OutputType { get; } = outputType;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public VariantValue Invoke() => func.Invoke();

    /// <inheritdoc />
    public override string ToString() => nameof(FuncUnitDelegate);
}
