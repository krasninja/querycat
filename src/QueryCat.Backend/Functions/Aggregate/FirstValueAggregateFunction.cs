using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Implements "FIRST_VALUE" aggregation function.
/// </summary>
[SafeFunction]
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
    public static IAggregateFunction CreateInstance() => new FirstValueAggregateFunction();

    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) => [VariantValue.Null];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, IExecutionThread thread)
    {
        if (!state[0].IsNull)
        {
            return;
        }

        var value = thread.Stack[0];
        if (!value.IsNull)
        {
            state[0] = value;
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state) => state[0];
}
