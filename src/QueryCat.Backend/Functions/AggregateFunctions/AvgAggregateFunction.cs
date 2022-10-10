using System.ComponentModel;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Implements "average" aggregation function.
/// </summary>
[Description("Computes the average value.")]
[AggregateFunctionSignature("avg(value: integer): float")]
[AggregateFunctionSignature("avg(value: float): float")]
[AggregateFunctionSignature("avg(value: numeric): numeric")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class AvgAggregateFunction : IAggregateFunction
{
    /// <inheritdoc />
    public VariantValueArray GetInitialState(DataType type)
        => new(
            new VariantValue(type), // 0: sum
            new VariantValue(DataType.Integer) // 1: count
        );

    /// <inheritdoc />
    public void Invoke(VariantValueArray state, FunctionCallInfo callInfo)
    {
        var value = callInfo.GetAt(0);
        if (!value.IsNull)
        {
            state.Values[0] += value;
            state.Values[1] =
                VariantValue.Add(ref state.Values[1], ref VariantValue.OneIntegerValue, out ErrorCode _);
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValueArray state)
    {
        var sum = state.Values[0];
        var count = state.Values[1];

        if (count == 0)
        {
            return VariantValue.Null;
        }

        return new VariantValue(sum / (double)count.AsInteger);
    }
}
