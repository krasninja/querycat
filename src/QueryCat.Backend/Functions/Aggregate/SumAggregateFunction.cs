using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Implements "summary" aggregation function.
/// </summary>
[SafeFunction]
[Description("Computes the sum of values.")]
[AggregateFunctionSignature("sum(value: integer): integer")]
[AggregateFunctionSignature("sum(value: float): float")]
[AggregateFunctionSignature("sum(value: numeric): numeric")]
[AggregateFunctionSignature("sum(value: interval): interval")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class SumAggregateFunction : IAggregateFunction
{
    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) => [VariantValue.Null];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, FunctionCallInfo callInfo)
    {
        var value = callInfo.GetAt(0);
        AggregateFunctionsUtils.ExecuteWithNullInitialState(ref state[0], in value, VariantValue.Add);
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state) => state[0];
}
