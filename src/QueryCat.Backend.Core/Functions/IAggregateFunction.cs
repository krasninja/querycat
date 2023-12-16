using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Aggregate function. The aggregate function is the special type of functions
/// that process on rows set column instead of specific arguments.
/// </summary>
public interface IAggregateFunction
{
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
    /// <param name="callInfo">Function call info. Arguments.</param>
    void Invoke(VariantValue[] state, FunctionCallInfo callInfo);

    /// <summary>
    /// Get the current aggregate result.
    /// </summary>
    /// <param name="state">State.</param>
    /// <returns>Aggregation result.</returns>
    VariantValue GetResult(VariantValue[] state);
}
