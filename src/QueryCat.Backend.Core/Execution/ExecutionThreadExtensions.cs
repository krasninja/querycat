using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Extensions for <see cref="IExecutionThread" />.
/// </summary>
public static class ExecutionThreadExtensions
{
    #region Variables

    /// <summary>
    /// Check whether a variable with name exists in the scope.
    /// </summary>
    /// <param name="executionThread">Instance of <see cref="IExecutionThread" />.</param>
    /// <param name="name">Variable name.</param>
    /// <returns>True if variable with the specified name is found, false otherwise.</returns>
    public static bool ContainsVariable(this IExecutionThread executionThread, string name)
        => executionThread.TopScope.TryGetVariable(name, out _);

    /// <summary>
    /// Get variable value from top scope to the root recursively.
    /// </summary>
    /// <param name="executionThread">Instance of <see cref="IExecutionThread" />.</param>
    /// <param name="name">Variable name.</param>
    /// <returns>Value or NULL if not found.</returns>
    public static VariantValue GetVariable(this IExecutionThread executionThread, string name)
    {
        if (executionThread.TopScope.TryGetVariable(name, out var value))
        {
            return value;
        }
        return VariantValue.Null;
    }

    #endregion
}
