using System.Collections;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Function call information: arguments values, execution scope.
/// </summary>
public sealed class FunctionCallInfo : IEnumerable<VariantValue>
{
    private const string UndefinedFunctionName = "self";

    private readonly VariantValueArray _args;
    private int _argsCursor;
    private readonly IFuncUnit[] _pushArgs;

    public static FunctionCallInfo Empty { get; } = new(NullExecutionThread.Instance, UndefinedFunctionName);

    /// <summary>
    /// Current execution thread.
    /// </summary>
    public IExecutionThread ExecutionThread { get; }

    /// <summary>
    /// Window information (optional).
    /// </summary>
    public IWindowInfo? WindowInfo { get; internal set; }

    /// <summary>
    /// Arguments count.
    /// </summary>
    public int Count => _args.Values.Length;

    /// <summary>
    /// Calling function name.
    /// </summary>
    public string FunctionName { get; }

    public static FunctionCallInfo CreateWithArguments(IExecutionThread executionThread, params VariantValue[] args)
    {
        var callInfo = new FunctionCallInfo(executionThread, UndefinedFunctionName);
        foreach (var arg in args)
        {
            callInfo.Push(arg);
        }
        return callInfo;
    }

    public static FunctionCallInfo CreateWithArguments(IExecutionThread executionThread, params object[] args)
    {
        var callInfo = new FunctionCallInfo(executionThread, UndefinedFunctionName);
        foreach (var arg in args)
        {
            callInfo.Push(VariantValue.CreateFromObject(arg));
        }
        return callInfo;
    }

    public FunctionCallInfo(IExecutionThread executionThread, string functionName, params IFuncUnit[] pushArgs)
    {
        _pushArgs = pushArgs;
        _args = new VariantValueArray(pushArgs.Length);
        ExecutionThread = executionThread;
        FunctionName = functionName;
    }

    /// <summary>
    /// Push new value to argument stack.
    /// </summary>
    /// <param name="value">Argument value.</param>
    public void Push(VariantValue value)
    {
        _argsCursor++;
        if (_argsCursor > _args.Values.Length)
        {
            _args.EnsureResize(_argsCursor);
        }
        _args.Values[_argsCursor - 1] = value;
    }

    /// <summary>
    /// Return argument at the specified index.
    /// </summary>
    /// <param name="position">Position index.</param>
    /// <returns>Value.</returns>
    public VariantValue GetAt(int position) => _args.Values[position];

    /// <summary>
    /// Return argument at the specified index or default value.
    /// </summary>
    /// <param name="position">Position index.</param>
    /// <param name="default">Default value.</param>
    /// <returns>Value.</returns>
    public VariantValue GetAtOrDefault(int position, VariantValue @default = default)
        => _args.Values.Length > position && position > -1 ? _args.Values[position] : @default;

    /// <summary>
    /// Clean current arguments stack.
    /// </summary>
    public void Reset()
    {
        _argsCursor = 0;
        WindowInfo = null;
    }

    internal void InvokePushArgs()
    {
        for (var i = 0; i < _pushArgs.Length; i++)
        {
            _args.Values[i] = _pushArgs[i].Invoke();
        }
    }

    /// <inheritdoc />
    public IEnumerator<VariantValue> GetEnumerator()
    {
        foreach (var arg in _args.Values)
        {
            yield return arg;
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public override string ToString() => _args.ToString();
}
