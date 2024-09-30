using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Implements "minimum" aggregation function.
/// </summary>
[SafeFunction]
[Description("Computes the minimum value.")]
[AggregateFunctionSignature("min(value: integer): integer")]
[AggregateFunctionSignature("min(value: float): float")]
[AggregateFunctionSignature("min(value: numeric): numeric")]
[AggregateFunctionSignature("min(value: timestamp): timestamp")]
[AggregateFunctionSignature("min(value: interval): interval")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class MinAggregateFunction : IAggregateFunction
{
    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) => [VariantValue.Null];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, FunctionCallInfo callInfo)
    {
        var value = callInfo.GetAt(0);
        var comparer = VariantValue.GetLessDelegate(value.Type, state[0].Type);
        if (state[0].IsNull)
        {
            state[0] = value;
        }
        else if (comparer.Invoke(in value, in state[0]).AsBoolean)
        {
            state[0] = value;
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state) => state[0];
}
