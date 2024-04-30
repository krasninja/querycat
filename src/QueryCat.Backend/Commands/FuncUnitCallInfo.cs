using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Commands;

internal sealed class FuncUnitCallInfo : FunctionCallInfo
{
    private readonly IFuncUnit[] _pushArgs;

    public static new FuncUnitCallInfo Empty { get; } = new(NullExecutionThread.Instance, UndefinedFunctionName);

    public FuncUnitCallInfo(FunctionCallInfo functionCallInfo)
        : base(functionCallInfo.ExecutionThread, functionCallInfo.FunctionName)
    {
        _pushArgs = Array.Empty<IFuncUnit>();
        Arguments.AddRange(functionCallInfo);
    }

    /// <inheritdoc />
    public FuncUnitCallInfo(IExecutionThread executionThread, string functionName, params IFuncUnit[] pushArgs)
        : base(executionThread, functionName)
    {
        _pushArgs = pushArgs;
    }

    internal void InvokePushArgs()
    {
        Arguments.Clear();
        foreach (var pushArg in _pushArgs)
        {
            Arguments.Add(pushArg.Invoke());
        }
    }
}
