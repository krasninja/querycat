using System.ComponentModel;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Implements "summary" aggregation function.
/// </summary>
[Description("Computes the sum of values.")]
[AggregateFunctionSignature("sum(value: integer): integer")]
[AggregateFunctionSignature("sum(value: float): float")]
[AggregateFunctionSignature("sum(value: numeric): numeric")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class SumAggregateFunction : IAggregateFunction
{
    /// <inheritdoc />
    public VariantValueArray GetInitialState(DataType type)
        => new(VariantValue.Null);

    /// <inheritdoc />
    public void Invoke(VariantValueArray state, FunctionCallInfo callInfo)
    {
        var value = callInfo.GetAt(0);
        AggregateFunctionsUtils.ExecuteWithNullInitialState(ref state.Values[0], in value, VariantValue.Add);
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValueArray state) => state.Values[0];
}
