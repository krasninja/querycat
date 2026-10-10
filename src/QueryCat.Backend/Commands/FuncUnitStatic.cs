using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncUnitStatic(VariantValue value) : IFuncUnit
{
    private readonly ValueTask<VariantValue> _valueTaskValue = new(value);

    /// <inheritdoc />
    public DataType OutputType => value.Type;

    /// <inheritdoc />
    public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        => _valueTaskValue;

    /// <inheritdoc />
    public override string ToString() => $"{nameof(FuncUnitStatic)}: {value}";
}
