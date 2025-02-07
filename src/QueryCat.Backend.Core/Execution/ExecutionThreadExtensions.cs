using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Extensions for <see cref="IExecutionThread" />.
/// </summary>
public static class ExecutionThreadExtensions
{
    /// <summary>
    /// Run query with object properties as parameters.
    /// </summary>
    /// <param name="executionThread">Execution thread.</param>
    /// <param name="query">Query.</param>
    /// <param name="parameters">Object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Return value.</returns>
    public static Task<VariantValue> RunWithScopeAsync(
        this IExecutionThread executionThread,
        string query,
        object?[] parameters,
        CancellationToken cancellationToken = default)
    {
        var parametersDict = new Dictionary<string, VariantValue>();
        foreach (var parameter in parameters)
        {
            if (parameter == null)
            {
                continue;
            }
            var executionParameters = parameter.GetType()
                .GetProperties()
                .ToDictionary(p => p.Name, p => VariantValue.CreateFromObject(p.GetValue(parameter)));
            foreach (var executionParameter in executionParameters)
            {
                parametersDict[executionParameter.Key] = executionParameter.Value;
            }
        }

        return executionThread.RunAsync(query, parametersDict, cancellationToken);
    }

    #region Variables

    /// <summary>
    /// Check whether a variable with name exists in the scope.
    /// </summary>
    /// <param name="executionThread">Instance of <see cref="IExecutionThread" />.</param>
    /// <param name="name">Variable name.</param>
    /// <param name="scope">Scope instance.</param>
    /// <returns>True if variable with the specified name is found, false otherwise.</returns>
    public static bool ContainsVariable(this IExecutionThread executionThread, string name, IExecutionScope? scope = null)
        => executionThread.TryGetVariable(name, out _, scope);

    /// <summary>
    /// Get variable value from top scope to the root recursively.
    /// </summary>
    /// <param name="executionThread">Instance of <see cref="IExecutionThread" />.</param>
    /// <param name="scope">Scope instance.</param>
    /// <param name="name">Variable name.</param>
    /// <returns>Value or NULL if not found.</returns>
    public static VariantValue GetVariable(this IExecutionThread executionThread, string name, IExecutionScope? scope = null)
    {
        if (executionThread.TryGetVariable(name, out var value, scope))
        {
            return value;
        }
        return VariantValue.Null;
    }

    #endregion
}
