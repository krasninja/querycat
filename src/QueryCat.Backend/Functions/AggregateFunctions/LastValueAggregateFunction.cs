using System.ComponentModel;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Implements "LAST_VALUE" aggregation function.
/// </summary>
[Description("Returns value evaluated at the row that is the last row of the window frame.")]
[AggregateFunctionSignature("last_value(value: integer): integer")]
[AggregateFunctionSignature("last_value(value: numeric): numeric")]
[AggregateFunctionSignature("last_value(value: float): float")]
[AggregateFunctionSignature("last_value(value: string): string")]
[AggregateFunctionSignature("last_value(value: timestamp): timestamp")]
[AggregateFunctionSignature("last_value(value: interval): interval")]
[AggregateFunctionSignature("last_value(value: boolean): boolean")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class LastValueAggregateFunction : IAggregateFunction
{
    /// <inheritdoc />
    public VariantValueArray GetInitialState(DataType type)
        => new(VariantValue.Null);

    /// <inheritdoc />
    public void Invoke(VariantValueArray state, FunctionCallInfo callInfo)
    {
        var value = callInfo.GetAt(0);
        if (!value.IsNull)
        {
            state.Values[0] = value;
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValueArray state)
        => state.Values[0];
}
