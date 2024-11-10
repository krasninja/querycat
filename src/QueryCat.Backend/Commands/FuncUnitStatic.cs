using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncUnitStatic(VariantValue value) : IFuncUnit
{
    /// <inheritdoc />
    public DataType OutputType => value.Type;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public VariantValue Invoke(IExecutionThread thread) => value;

    /// <inheritdoc />
    public override string ToString() => $"{nameof(FuncUnitStatic)}: {value}";
}
