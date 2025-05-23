using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Implements "maximum" aggregation function.
/// </summary>
[SafeFunction]
[Description("Computes the maximum value.")]
[AggregateFunctionSignature("max(value: integer): integer")]
[AggregateFunctionSignature("max(value: float): float")]
[AggregateFunctionSignature("max(value: numeric): numeric")]
[AggregateFunctionSignature("max(value: timestamp): timestamp")]
[AggregateFunctionSignature("max(value: interval): interval")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class MaxAggregateFunction : IAggregateFunction
{
    /// <inheritdoc />
    public static IAggregateFunction CreateInstance() => new MaxAggregateFunction();

    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) => [VariantValue.Null];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, IExecutionThread thread)
    {
        var value = thread.Stack[0];
        var comparer = VariantValue.GetGreaterDelegate(value.Type, state[0].Type);
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
