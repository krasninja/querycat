using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// The execution scope contains variables, variables names and parent scope.
/// </summary>
public interface IExecutionScope
{
    /// <summary>
    /// The dictionary contains variable name and its value.
    /// </summary>
    IDictionary<string, VariantValue> Variables { get; }

    /// <summary>
    /// Parent scope. If null it is the root scope.
    /// </summary>
    IExecutionScope? Parent { get; }

    /// <summary>
    /// Try to get variable value from top scope to the root recursively.
    /// </summary>
    /// <param name="name">Variable name.</param>
    /// <param name="value">Variable value.</param>
    /// <returns><c>True</c> if variable with the specified name is found, <c>false</c> otherwise.</returns>
    bool TryGetVariable(string name, out VariantValue value);

    /// <summary>
    /// Try to set variable value.
    /// </summary>
    /// <param name="name">Variable name.</param>
    /// <param name="value">Variable value.</param>
    /// <returns><c>True</c> if variable with the specified value is set, <c>false</c> otherwise.</returns>
    bool TrySetVariable(string name, VariantValue value);
}
