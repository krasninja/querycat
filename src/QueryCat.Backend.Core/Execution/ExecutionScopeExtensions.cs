using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Extensions for <see cref="IExecutionScope" />.
/// </summary>
public static class ExecutionScopeExtensions
{
    /// <summary>
    /// Try get variable value from top scope to the root recursively.
    /// </summary>
    /// <param name="scope">Scope instance.</param>
    /// <param name="name">Variable name.</param>
    /// <param name="value">Variable value.</param>
    /// <returns>True if variable with the specified name is found, false otherwise.</returns>
    public static bool TryGet(this IExecutionScope scope, string name, out VariantValue value)
    {
        var currentScope = scope;
        while (currentScope != null)
        {
            if (currentScope.Variables.TryGetValue(name, out value))
            {
                return true;
            }
            currentScope = currentScope.Parent;
        }

        value = VariantValue.Null;
        return false;
    }

    /// <summary>
    /// Get variable value from top scope to the root recursively.
    /// </summary>
    /// <param name="scope">Scope instance.</param>
    /// <param name="name">Variable name.</param>
    /// <returns>Value or NULL if not found.</returns>
    public static VariantValue Get(this IExecutionScope scope, string name)
    {
        if (TryGet(scope, name, out var value))
        {
            return value;
        }
        return VariantValue.Null;
    }
}
