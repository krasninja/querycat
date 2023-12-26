using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncUnitStatic(VariantValue value) : IFuncUnit
{
    /// <inheritdoc />
    public DataType OutputType => value.GetInternalType();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public VariantValue Invoke() => value;

    /// <inheritdoc />
    public override string ToString() => $"{nameof(FuncUnitStatic)}: {value}";
}
