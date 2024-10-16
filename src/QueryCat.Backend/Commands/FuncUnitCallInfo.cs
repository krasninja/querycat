using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Commands;

internal sealed class FuncUnitCallInfo : FunctionCallInfo
{
    private readonly IFuncUnit[] _pushArgs;

    public static new FuncUnitCallInfo Empty { get; } = new(NullExecutionThread.Instance);

    /// <inheritdoc />
    public FuncUnitCallInfo(IExecutionThread executionThread, params IFuncUnit[] pushArgs)
        : base(executionThread)
    {
        _pushArgs = pushArgs;
    }

    internal void InvokePushArgs()
    {
        foreach (var pushArg in _pushArgs)
        {
            ExecutionThread.Stack.Push(pushArg.Invoke(ExecutionThread));
        }
    }
}
