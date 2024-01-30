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
}
