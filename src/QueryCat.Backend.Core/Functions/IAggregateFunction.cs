using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Aggregate function. The aggregate function is the special type of functions
/// that process on rows set column instead of specific arguments. Aggregate class must be stateless.
/// Instead, the "VariantValue[] state" should be used to store execution state.
/// </summary>
public interface IAggregateFunction
{
    /// <summary>
    /// Initialize the context. The function is called once before values processing.
    /// </summary>
    /// <returns>Initial state.</returns>
    static virtual IAggregateFunction CreateInstance() => NullAggregateFunction.Instance;

    /// <summary>
    /// Initialize the context. The function is called once before values processing.
    /// </summary>
    /// <param name="type">Target data type.</param>
    /// <returns>Initial state.</returns>
    VariantValue[] GetInitialState(DataType type);

    /// <summary>
    /// Process the value. The function is called on every next row value.
    /// </summary>
    /// <param name="state">State.</param>
    /// <param name="thread">Execution thread.</param>
    void Invoke(VariantValue[] state, IExecutionThread thread);

    /// <summary>
    /// Get the current aggregate result.
    /// </summary>
    /// <param name="state">State.</param>
    /// <returns>Aggregation result.</returns>
    VariantValue GetResult(VariantValue[] state);
}
