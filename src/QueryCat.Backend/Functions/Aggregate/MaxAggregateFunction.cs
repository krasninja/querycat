using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Implements "maximum" aggregation function.
/// </summary>
[Description("Computes the maximum value.")]
[AggregateFunctionSignature("max(value: integer): integer")]
[AggregateFunctionSignature("max(value: float): float")]
[AggregateFunctionSignature("max(value: numeric): numeric")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class MaxAggregateFunction : IAggregateFunction
{
    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) => new[] { VariantValue.Null };

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, FunctionCallInfo callInfo)
    {
        var value = callInfo.GetAt(0);
        if (state[0].IsNull)
        {
            state[0] = value;
        }
        else if (value > state[0])
        {
            state[0] = value;
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state) => state[0];
}
