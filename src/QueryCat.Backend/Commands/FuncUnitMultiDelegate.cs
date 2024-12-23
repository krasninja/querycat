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
    public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        var lastResult = VariantValue.Null;
        foreach (var funcUnit in funcUnits)
        {
            lastResult = await funcUnit.InvokeAsync(thread, cancellationToken);
        }
        return lastResult;
    }

    /// <inheritdoc />
    public override string ToString() => nameof(FuncUnitMultiDelegate);
}
