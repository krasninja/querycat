using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The execution scope contains variables, variables names and parent scope.
/// </summary>
public sealed class ExecutionScope : IExecutionScope
{
    private readonly ExecutionScope? _parent;

    private sealed class CaseInsensitiveEqualityComparer : IEqualityComparer<string>
    {
        public static CaseInsensitiveEqualityComparer Instance { get; } = new();

        /// <inheritdoc />
        public bool Equals(string? x, string? y)
        {
            if (x == y)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            return x.Equals(y, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public int GetHashCode(string obj) => obj.ToUpper().GetHashCode();
    }

    /// <summary>
    /// Variables array.
    /// </summary>
    public IDictionary<string, VariantValue> Variables { get; }
        = new Dictionary<string, VariantValue>(CaseInsensitiveEqualityComparer.Instance);

    /// <inheritdoc />
    public IExecutionScope? Parent => _parent;

    public ExecutionScope(ExecutionScope? parent = null)
    {
        _parent = parent;
    }
}
