using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The execution scope contains variables, variables names and parent scope.
/// </summary>
public sealed class ExecutionScope
{
    private readonly List<VariantValue> _variables = new();
    private readonly Dictionary<string, int> _variablesNames = new();
    private readonly ExecutionScope? _parent;

    internal SelectQueryExpressionBodyNode? Query { get; set; }

    public IReadOnlyList<VariantValue> Variables => _variables;

    public IReadOnlyDictionary<string, int> VariablesNames => _variablesNames;

    public ExecutionScope(ExecutionScope? parent = null)
    {
        _parent = parent;
    }

    public Func<VariantValue> GetIdentifier(string name)
    {
        var currentScope = this;
        while (currentScope != null)
        {
            if (currentScope.VariablesNames.TryGetValue(name, out int varIndex))
            {
                return () => Variables[varIndex];
            }
            currentScope = currentScope._parent;
        }
        throw new CannotFindIdentifierException(name);
    }

    public void DefineVariable(string name, DataType type)
    {
        _variables.Add(new VariantValue(type));
        _variablesNames.Add(name, _variables.Count - 1);
    }
}
