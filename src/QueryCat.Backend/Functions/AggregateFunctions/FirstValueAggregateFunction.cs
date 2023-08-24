using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Implements "FIRST_VALUE" aggregation function.
/// </summary>
[Description("Returns value evaluated at the row that is the first row of the window frame.")]
[AggregateFunctionSignature("first_value(value: integer): integer")]
[AggregateFunctionSignature("first_value(value: numeric): numeric")]
[AggregateFunctionSignature("first_value(value: float): float")]
[AggregateFunctionSignature("first_value(value: string): string")]
[AggregateFunctionSignature("first_value(value: timestamp): timestamp")]
[AggregateFunctionSignature("first_value(value: interval): interval")]
[AggregateFunctionSignature("first_value(value: boolean): boolean")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class FirstValueAggregateFunction : IAggregateFunction
{
    /// <inheritdoc />
    public VariantValueArray GetInitialState(DataType type)
        => new(VariantValue.Null);

    /// <inheritdoc />
    public void Invoke(VariantValueArray state, FunctionCallInfo callInfo)
    {
        if (!state.Values[0].IsNull)
        {
            return;
        }

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
