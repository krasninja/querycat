using System.Diagnostics;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The execution scope contains variables, variables names and parent scope.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
public sealed class DefaultExecutionScope : IExecutionScope
{
    private readonly IExecutionScope? _parent;

    /// <summary>
    /// Variables array.
    /// </summary>
    public IDictionary<string, VariantValue> Variables { get; }
        = new Dictionary<string, VariantValue>(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Count of variables in the scope.
    /// </summary>
    public int Count => Variables.Count;

    /// <inheritdoc />
    public IExecutionScope? Parent => _parent;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="parent">Parent execution scope.</param>
    public DefaultExecutionScope(IExecutionScope? parent)
    {
        _parent = parent;
    }

    /// <inheritdoc />
    public bool TryGetVariable(string name, out VariantValue value)
    {
        var currentScope = (IExecutionScope)this;
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

    /// <inheritdoc />
    public bool TrySetVariable(string name, VariantValue value)
    {
        var currentScope = (IExecutionScope)this;
        while (currentScope != null)
        {
            if (currentScope.Variables.ContainsKey(name))
            {
                currentScope.Variables[name] = value;
                return true;
            }
            currentScope = currentScope.Parent;
        }

        Variables[name] = value;
        return true;
    }
}
