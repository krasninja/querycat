using System.Collections;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Function call information: arguments values, execution scope.
/// </summary>
public class FunctionCallInfo : IEnumerable<VariantValue>
{
    internal const string UndefinedFunctionName = "self";

    private readonly List<VariantValue> _args;

    public static FunctionCallInfo Empty { get; } = new(NullExecutionThread.Instance, UndefinedFunctionName);

    protected List<VariantValue> Arguments => _args;

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
    public int Count => _args.Count;

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

    public FunctionCallInfo(IExecutionThread executionThread, string functionName, params VariantValue[] args)
    {
        _args = new List<VariantValue>(args);
        ExecutionThread = executionThread;
        FunctionName = functionName;
    }

    /// <summary>
    /// Push new value to argument stack.
    /// </summary>
    /// <param name="value">Argument value.</param>
    public void Push(VariantValue value)
    {
        _args.Add(value);
    }

    /// <summary>
    /// Return argument at the specified index.
    /// </summary>
    /// <param name="position">Position index.</param>
    /// <returns>Value.</returns>
    public VariantValue GetAt(int position) => _args[position];

    /// <summary>
    /// Return argument at the specified index or default value.
    /// </summary>
    /// <param name="position">Position index.</param>
    /// <param name="default">Default value.</param>
    /// <returns>Value.</returns>
    public VariantValue GetAtOrDefault(int position, VariantValue @default = default)
        => _args.Count > position && position > -1 ? _args[position] : @default;

    /// <summary>
    /// Clean current arguments stack.
    /// </summary>
    public void Reset()
    {
        _args.Clear();
        WindowInfo = null;
    }

    /// <inheritdoc />
    public IEnumerator<VariantValue> GetEnumerator()
    {
        foreach (var arg in _args)
        {
            yield return arg;
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public override string ToString() => string.Join("; ", _args.Select(a => a.ToString()));
}
