using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;

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

    internal void InvokePushArgs(IExecutionThread thread)
    {
        foreach (var pushArg in _pushArgs)
        {
            thread.Stack.Push(pushArg.Invoke(thread));
        }
    }
}
