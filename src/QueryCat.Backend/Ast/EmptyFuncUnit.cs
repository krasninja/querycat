using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast;

internal sealed class EmptyFuncUnit : IFuncUnit
{
    public static EmptyFuncUnit Instance { get; } = new();

    /// <inheritdoc />
    public DataType OutputType => DataType.Null;

    /// <inheritdoc />
    public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(VariantValue.Null);
}
