using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core;

/// <summary>
/// Extensions for <see cref="IExecutionThread" />.
/// </summary>
public static class ExecutionThreadExtensions
{
    /// <summary>
    /// Call function within execution thread.
    /// </summary>
    /// <param name="executionThread">Execution thread.</param>
    /// <param name="functionDelegate">Function delegate instance.</param>
    /// <param name="args">Arguments.</param>
    /// <returns>Return value.</returns>
    public static VariantValue CallFunction(
        this IExecutionThread executionThread, FunctionDelegate functionDelegate, params object[] args)
    {
        var functionCallInfo = FunctionCallInfo.CreateWithArguments(executionThread, args);
        return functionDelegate.Invoke(functionCallInfo);
    }
}
