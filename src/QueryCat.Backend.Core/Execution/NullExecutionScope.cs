using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Execution scope that does nothing.
/// </summary>
public sealed class NullExecutionScope : IExecutionScope
{
    public static NullExecutionScope Instance { get; } = new();

    /// <inheritdoc />
    public IDictionary<string, VariantValue> Variables { get; } = new Dictionary<string, VariantValue>();

    /// <inheritdoc />
    public IExecutionScope? Parent => null;
}
