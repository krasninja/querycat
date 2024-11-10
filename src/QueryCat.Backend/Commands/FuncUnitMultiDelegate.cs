using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncUnitMultiDelegate(DataType outputType, params IFuncUnit[] funcUnits) : IFuncUnit
{
    /// <inheritdoc />
    public DataType OutputType { get; } = outputType;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public VariantValue Invoke(IExecutionThread thread)
    {
        var lastResult = VariantValue.Null;
        foreach (var funcUnit in funcUnits)
        {
            lastResult = funcUnit.Invoke(thread);
        }
        return lastResult;
    }

    /// <inheritdoc />
    public override string ToString() => nameof(FuncUnitMultiDelegate);
}
