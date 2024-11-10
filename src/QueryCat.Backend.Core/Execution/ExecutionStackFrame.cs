using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Handy wrapper for execution stack.
/// </summary>
/// <param name="stack">Instance of <see cref="IExecutionStack" />.</param>
public readonly struct ExecutionStackFrame(IExecutionStack stack) : IDisposable
{
    /// <summary>
    /// Push value to the current frame.
    /// </summary>
    /// <param name="value">Value to push.</param>
    public void Push(VariantValue value) => stack.Push(value);

    /// <summary>
    /// Pop value from the current frame.
    /// </summary>
    /// <returns>Returned value.</returns>
    public VariantValue Pop() => stack.Pop();

    /// <inheritdoc />
    public void Dispose()
    {
        stack.CloseFrame();
    }
}
