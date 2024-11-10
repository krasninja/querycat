using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Extensions for <see cref="IExecutionStack" />.
/// </summary>
public static class ExecutionStackExtensions
{
    /// <summary>
    /// Return argument at the specified index or default value.
    /// </summary>
    /// <param name="executionStack">Instance of <see cref="IExecutionStack" />.</param>
    /// <param name="position">Position index.</param>
    /// <param name="default">Default value.</param>
    /// <returns>Value.</returns>
    public static VariantValue GetAtOrDefault(
        this IExecutionStack executionStack,
        int position,
        VariantValue @default = default)
        => executionStack.FrameLength > position && position > -1 ? executionStack[position] : @default;

    /// <summary>
    /// Create frame with arguments.
    /// </summary>
    /// <param name="executionStack">Instance of <see cref="IExecutionStack" />.</param>
    /// <param name="arguments">Arguments.</param>
    /// <returns>Stack frame.</returns>
    public static ExecutionStackFrame CreateFrameWithArguments(
        this IExecutionStack executionStack,
        params VariantValue[] arguments)
    {
        var frame = executionStack.CreateFrame();
        foreach (var arg in arguments)
        {
            executionStack.Push(VariantValue.CreateFromObject(arg));
        }
        return frame;
    }
}
