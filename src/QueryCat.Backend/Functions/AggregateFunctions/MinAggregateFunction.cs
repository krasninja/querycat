using System.ComponentModel;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Implements "minimum" aggregation function.
/// </summary>
[Description("Computes the minimum value.")]
[AggregateFunctionSignature("min(value: integer): integer")]
[AggregateFunctionSignature("min(value: float): float")]
[AggregateFunctionSignature("min(value: numeric): numeric")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class MinAggregateFunction : IAggregateFunction
{
    /// <inheritdoc />
    public VariantValueArray GetInitialState(DataType type)
        => new(VariantValue.Null);

    /// <inheritdoc />
    public void Invoke(VariantValueArray state, FunctionCallInfo callInfo)
    {
        var value = callInfo.GetAt(0);
        if (state.Values[0].IsNull)
        {
            state.Values[0] = value;
        }
        else if (value < state.Values[0])
        {
            state.Values[0] = value;
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValueArray state) => state.Values[0];
}
