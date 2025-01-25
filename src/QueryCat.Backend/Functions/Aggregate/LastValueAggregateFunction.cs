using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Implements "LAST_VALUE" aggregation function.
/// </summary>
[SafeFunction]
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
    public static IAggregateFunction CreateInstance() => new LastValueAggregateFunction();

    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) => [VariantValue.Null];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, IExecutionThread thread)
    {
        var value = thread.Stack[0];
        if (!value.IsNull)
        {
            state[0] = value;
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state) => state[0];
}
