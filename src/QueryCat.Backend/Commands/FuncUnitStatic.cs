using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncUnitStatic(VariantValue value) : IFuncUnit
{
    /// <inheritdoc />
    public DataType OutputType => value.Type;

    /// <inheritdoc />
    public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(value);

    /// <inheritdoc />
    public override string ToString() => $"{nameof(FuncUnitStatic)}: {value}";
}
