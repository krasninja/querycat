using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncCommandHandler : IFuncUnit
{
    private readonly Func<IExecutionThread, CancellationToken, ValueTask<VariantValue>> _func;

    public static FuncCommandHandler NullHandler { get; } = new((thread, ct) => ValueTask.FromResult(VariantValue.Null));

    public FuncCommandHandler(Func<IExecutionThread, CancellationToken, ValueTask<VariantValue>> func)
    {
        _func = func;
    }

    /// <inheritdoc />
    public DataType OutputType => DataType.Null;

    /// <inheritdoc />
    public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        => _func.Invoke(thread, cancellationToken);
}
