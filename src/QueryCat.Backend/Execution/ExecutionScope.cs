using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The execution scope contains variables, variables names and parent scope.
/// </summary>
public sealed class ExecutionScope : IExecutionScope
{
    private readonly ExecutionScope? _parent;

    /// <summary>
    /// Variables array.
    /// </summary>
    public IDictionary<string, VariantValue> Variables { get; }
        = new Dictionary<string, VariantValue>(StringComparer.InvariantCultureIgnoreCase);

    /// <inheritdoc />
    public IExecutionScope? Parent => _parent;

    public ExecutionScope(ExecutionScope? parent = null)
    {
        _parent = parent;
    }
}
