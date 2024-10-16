using System.Collections;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Function call information: arguments values, execution scope.
/// </summary>
public class FunctionCallInfo : IEnumerable<VariantValue>
{
    /// <summary>
    /// Empty instance of <see cref="FunctionCallInfo" />.
    /// </summary>
    public static FunctionCallInfo Empty { get; } = new(NullExecutionThread.Instance);

    /// <summary>
    /// Current execution thread.
    /// </summary>
    public IExecutionThread ExecutionThread { get; }

    /// <summary>
    /// Create instance of <see cref="FunctionCallInfo" /> from array of arguments.
    /// </summary>
    /// <param name="executionThread">Execution thread.</param>
    /// <param name="args">Arguments array.</param>
    /// <returns>Instance of <see cref="FunctionCallInfo" />.</returns>
    public static FunctionCallInfo CreateWithArguments(IExecutionThread executionThread, params VariantValue[] args)
    {
        return new FunctionCallInfo(executionThread, args);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="executionThread">Execution thread instance.</param>
    /// <param name="args">Call arguments.</param>
    public FunctionCallInfo(IExecutionThread executionThread, params VariantValue[] args)
    {
        ExecutionThread = executionThread;
        foreach (var arg in args)
        {
            ExecutionThread.Stack.Push(arg);
        }
    }

    /// <summary>
    /// Push new value to argument stack.
    /// </summary>
    /// <param name="value">Argument value.</param>
    public void Push(VariantValue value)
    {
        ExecutionThread.Stack.Push(value);
    }

    /// <summary>
    /// Return argument at the specified index.
    /// </summary>
    /// <param name="position">Position index.</param>
    /// <returns>Value.</returns>
    public VariantValue GetAt(int position) => ExecutionThread.Stack[position];

    /// <summary>
    /// Return argument at the specified index or default value.
    /// </summary>
    /// <param name="position">Position index.</param>
    /// <param name="default">Default value.</param>
    /// <returns>Value.</returns>
    public VariantValue GetAtOrDefault(int position, VariantValue @default = default)
        => ExecutionThread.Stack.GetAtOrDefault(position, @default);

    /// <inheritdoc />
    public IEnumerator<VariantValue> GetEnumerator() => ExecutionThread.Stack.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
