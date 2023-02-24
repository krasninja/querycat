using QueryCat.Backend.Execution;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Function call information: arguments values, execution scope.
/// </summary>
public sealed class FunctionCallInfo
{
    private readonly VariantValueArray _args;
    private int _argsCursor;
    private readonly IFuncUnit[] _pushArgs;

    public static FunctionCallInfo Empty { get; } = new(ExecutionThread.Empty);

    /// <summary>
    /// Current execution thread.
    /// </summary>
    public ExecutionThread ExecutionThread { get; }

    /// <summary>
    /// Window information (optional).
    /// </summary>
    public IWindowInfo? WindowInfo { get; internal set; }

    /// <summary>
    /// Function call arguments.
    /// </summary>
    public VariantValueArray Arguments => _args;

    public static FunctionCallInfo CreateWithArguments(ExecutionThread executionThread, params VariantValue[] args)
    {
        var callInfo = new FunctionCallInfo(executionThread);
        foreach (var arg in args)
        {
            callInfo.Push(arg);
        }
        return callInfo;
    }

    public static FunctionCallInfo CreateWithArguments(ExecutionThread executionThread, params object[] args)
    {
        var callInfo = new FunctionCallInfo(executionThread);
        foreach (var arg in args)
        {
            callInfo.Push(VariantValue.CreateFromObject(arg));
        }
        return callInfo;
    }

    public FunctionCallInfo(ExecutionThread executionThread, params IFuncUnit[] pushArgs)
    {
        _pushArgs = pushArgs;
        _args = new VariantValueArray(pushArgs.Length);
        ExecutionThread = executionThread;
    }

    public void Push(VariantValue value)
    {
        _argsCursor++;
        if (_argsCursor > _args.Values.Length)
        {
            _args.Resize(_argsCursor);
        }
        _args.Values[_argsCursor - 1] = value;
    }

    public VariantValue GetAt(int position) => _args.Values[position];

    /// <summary>
    /// Clean current arguments stack.
    /// </summary>
    public void Reset()
    {
        _argsCursor = 0;
        WindowInfo = null;
    }

    public void InvokePushArgs()
    {
        for (var i = 0; i < _pushArgs.Length; i++)
        {
            _args.Values[i] = _pushArgs[i].Invoke();
        }
    }

    /// <inheritdoc />
    public override string ToString() => _args.ToString();
}
