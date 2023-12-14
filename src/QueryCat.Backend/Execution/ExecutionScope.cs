using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The execution scope contains variables, variables names and parent scope.
/// </summary>
public sealed class ExecutionScope
{
    private readonly List<VariantValue> _variables = new();
    private readonly Dictionary<string, int> _variablesNames = new(StringComparer.CurrentCultureIgnoreCase);
    private readonly ExecutionScope? _parent;

    /// <summary>
    /// Variables array.
    /// </summary>
    internal IReadOnlyList<VariantValue> Variables => _variables;

    /// <summary>
    /// Map of variable name and index.
    /// </summary>
    internal IReadOnlyDictionary<string, int> VariablesNames => _variablesNames;

    public ExecutionScope(ExecutionScope? parent = null)
    {
        _parent = parent;
    }

    /// <summary>
    /// Get variable index by name.
    /// </summary>
    /// <param name="name">Variable name.</param>
    /// <param name="scope">Scope.</param>
    /// <returns>Variable index or -1 if not found.</returns>
    public int GetVariableIndex(string name, out ExecutionScope? scope)
    {
        var currentScope = this;
        while (currentScope != null)
        {
            if (currentScope.VariablesNames.TryGetValue(name, out int varIndex))
            {
                scope = currentScope;
                return varIndex;
            }
            currentScope = currentScope._parent;
        }
        scope = null;
        return -1;
    }

    /// <summary>
    /// Set variable value by index.
    /// </summary>
    /// <param name="index">Variable index in memory list.</param>
    /// <param name="value">New value.</param>
    public void SetVariable(int index, VariantValue value)
    {
        var sourceType = _variables[index].GetInternalType();
        _variables[index] = value.Cast(sourceType);
    }

    /// <summary>
    /// Define new variable.
    /// </summary>
    /// <param name="name">Variable name.</param>
    /// <param name="value">Optional value.</param>
    /// <returns>Variable index within scope.</returns>
    public int DefineVariable(string name, VariantValue value = default)
        => DefineVariable(name, value.GetInternalType(), value);

    /// <summary>
    /// Define new variable.
    /// </summary>
    /// <param name="name">Variable name.</param>
    /// <param name="value">Optional value.</param>
    /// <returns>Variable index within scope.</returns>
    public int DefineVariable(string name, object value)
        => DefineVariable(name, VariantValue.CreateFromObject(value));

    /// <summary>
    /// Define new variable.
    /// </summary>
    /// <param name="name">Variable name.</param>
    /// <param name="type">Variable type.</param>
    /// <param name="value">Optional value.</param>
    /// <returns>Variable index within scope.</returns>
    public int DefineVariable(string name, DataType type, VariantValue value = default)
    {
        if (_variablesNames.ContainsKey(name))
        {
            throw new QueryCatException($"The variable with name '{name}' is already declared.");
        }

        _variables.Add(new VariantValue(type));
        var index = _variables.Count - 1;
        _variablesNames.Add(name, index);
        if (!value.IsNull)
        {
            _variables[index] = value.Cast(type);
        }
        return index;
    }
}
