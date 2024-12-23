using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncUnitDelegate(Func<IExecutionThread, CancellationToken, ValueTask<VariantValue>> func, DataType outputType) : IFuncUnit
{
    public IReadOnlyList<IRowsIterator>? SubQueryIterators { get; set; }

    /// <inheritdoc />
    public DataType OutputType { get; } = outputType;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        => func.Invoke(thread, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => nameof(FuncUnitDelegate);
}
