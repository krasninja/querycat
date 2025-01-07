using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncUnitCallInfo
{
    private readonly IFuncUnit[] _pushArgs;

    public static FuncUnitCallInfo Empty { get; } = new();

    public bool IsEmpty => _pushArgs.Length == 0;

    public FuncUnitCallInfo(params IFuncUnit[] pushArgs)
    {
        _pushArgs = pushArgs;
    }

    internal async ValueTask<VariantValue[]> InvokePushArgsAsync(IExecutionThread thread,
        CancellationToken cancellationToken)
    {
        var values = new VariantValue[_pushArgs.Length];
        for (var i = 0; i < _pushArgs.Length; i++)
        {
            values[i] = await _pushArgs[i].InvokeAsync(thread, cancellationToken);
        }
        return values;
    }

    /// <inheritdoc />
    public override string ToString() => string.Join(';', _pushArgs.Select(a => a.ToString()));
}
