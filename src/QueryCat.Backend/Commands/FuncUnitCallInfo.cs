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

    internal VariantValue[] InvokePushArgs(IExecutionThread thread)
    {
        var values = new VariantValue[_pushArgs.Length];
        for (var i = 0; i < _pushArgs.Length; i++)
        {
            values[i] = _pushArgs[i].Invoke(thread);
        }
        return values;
    }

    /// <inheritdoc />
    public override string ToString() => string.Join(';', _pushArgs.Select(a => a.ToString()));
}
