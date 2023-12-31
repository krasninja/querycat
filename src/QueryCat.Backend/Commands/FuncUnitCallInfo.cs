using QueryCat.Backend.Core;
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
        for (var i = 0; i < _pushArgs.Length; i++)
        {
            Arguments.Add(_pushArgs[i].Invoke());
        }
    }
}
