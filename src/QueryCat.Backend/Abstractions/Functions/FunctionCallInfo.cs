using System.Collections;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Abstractions.Functions;

/// <summary>
/// Function call information: arguments values, execution scope.
/// </summary>
public sealed class FunctionCallInfo : IEnumerable<VariantValue>
{
    private readonly VariantValueArray _args;
    private int _argsCursor;
    private readonly IFuncUnit[] _pushArgs;

    public static FunctionCallInfo Empty { get; } = new(QueryCat.Backend.Execution.ExecutionThread.DefaultInstance);

    /// <summary>
    /// Current execution thread.
    /// </summary>
    public IExecutionThread ExecutionThread { get; }

    /// <summary>
    /// Window information (optional).
    /// </summary>
    public IWindowInfo? WindowInfo { get; internal set; }

    /// <summary>
    /// Function call arguments.
    /// </summary>
    public VariantValueArray Arguments => _args;

    /// <summary>
    /// Arguments count.
    /// </summary>
    public int Count => _args.Values.Length;

    /// <summary>
    /// Calling function name.
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

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

    public FunctionCallInfo(IExecutionThread executionThread, params IFuncUnit[] pushArgs)
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
        => _args.Values.Length > position ? _args.Values[position] : @default;

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
