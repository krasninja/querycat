using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

internal sealed class NullAggregateFunction : IAggregateFunction
{
    public static IAggregateFunction Instance { get; } = CreateInstance();

    /// <inheritdoc />
    public static IAggregateFunction CreateInstance() => new NullAggregateFunction();

    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) => [];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, IExecutionThread thread)
    {
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state) => default;
}
