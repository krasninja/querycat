using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncCommandHandler : IFuncUnit
{
    private readonly Func<IExecutionThread, VariantValue> _func;

    public static FuncCommandHandler NullHandler { get; } = new(thread => VariantValue.Null);

    public FuncCommandHandler(Func<IExecutionThread, VariantValue> func)
    {
        _func = func;
    }

    /// <inheritdoc />
    public DataType OutputType => DataType.Null;

    /// <inheritdoc />
    public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_func.Invoke(thread));
}
