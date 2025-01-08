using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Utilities for function calling.
/// </summary>
public static class FunctionCaller
{
    /// <summary>
    /// Call function within execution thread.
    /// </summary>
    /// <param name="delegate">Function delegate instance.</param>
    /// <param name="executionThread">Execution thread.</param>
    /// <param name="args">Arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Return value.</returns>
    public static ValueTask<VariantValue> CallWithArgumentsAsync(
        Delegate @delegate, IExecutionThread executionThread, object[]? args = null, CancellationToken cancellationToken = default)
    {
        using var frame = executionThread.Stack.CreateFrame();
        args ??= [];
        foreach (var arg in args)
        {
            frame.Push(VariantValue.CreateFromObject(arg));
        }
        return CallAsync(@delegate, executionThread, cancellationToken);
    }

    /// <summary>
    /// Call the function delegate.
    /// </summary>
    /// <param name="function">Function to call.</param>
    /// <param name="thread">Execution thread.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Value.</returns>
    internal static ValueTask<VariantValue> CallAsync(IFunction function, IExecutionThread thread, CancellationToken cancellationToken = default)
        => CallAsync(function.Delegate, thread, cancellationToken);

    /// <summary>
    /// Call the function delegate.
    /// </summary>
    /// <param name="delegate">Delegate to call.</param>
    /// <param name="thread">Execution thread.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Value.</returns>
    internal static async ValueTask<VariantValue> CallAsync(Delegate @delegate, IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        if (@delegate is Func<IExecutionThread, VariantValue> functionDelegate)
        {
            return functionDelegate.Invoke(thread);
        }
        if (@delegate is Func<IExecutionThread, CancellationToken, ValueTask<VariantValue>> functionDelegateAsync)
        {
            return await functionDelegateAsync.Invoke(thread, cancellationToken);
        }
        throw new InvalidOperationException(
            "The delegate type must be  Func<IExecutionThread, VariantValue> or Func<IExecutionThread, CancellationToken, ValueTask<VariantValue>>.");
    }

    /// <summary>
    /// Returns <c>true</c> if delegate is the valid QueryCat function.
    /// </summary>
    /// <param name="delegate">Delegate.</param>
    /// <returns>Returns <c>true</c> if valid.</returns>
    internal static bool IsValidFunctionDelegate(Delegate @delegate)
        => @delegate is Func<IExecutionThread, VariantValue>
           || @delegate is Func<IExecutionThread, CancellationToken, ValueTask<VariantValue>>;
}
