using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

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

    /// <summary>
    /// Run query with object properties as parameters.
    /// </summary>
    /// <param name="executionThread">Execution thread.</param>
    /// <param name="query">Query.</param>
    /// <param name="parameters">Object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Return value.</returns>
    public static VariantValue RunWithScope(
        this IExecutionThread executionThread,
        string query,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        var executionParameters = parameters.GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => VariantValue.CreateFromObject(p.GetValue(parameters)));
        return executionThread.Run(query, executionParameters, cancellationToken);
    }
}
