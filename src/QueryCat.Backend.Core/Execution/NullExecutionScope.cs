using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Execution scope that does nothing.
/// </summary>
public sealed class NullExecutionScope : IExecutionScope
{
    /// <summary>
    /// Static instance of <see cref="NullExecutionScope" />.
    /// </summary>
    public static NullExecutionScope Instance { get; } = new();

    /// <inheritdoc />
    public IDictionary<string, VariantValue> Variables { get; } = new Dictionary<string, VariantValue>();

    /// <inheritdoc />
    public IExecutionScope? Parent => null;

    /// <inheritdoc />
    public bool TryGetVariable(string name, out VariantValue value)
    {
        value = VariantValue.Null;
        return true;
    }

    /// <inheritdoc />
    public bool TrySetVariable(string name, VariantValue value) => true;
}
