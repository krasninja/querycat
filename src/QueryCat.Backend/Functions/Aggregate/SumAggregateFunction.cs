using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
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
    public static IAggregateFunction CreateInstance() => new SumAggregateFunction();

    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) => [VariantValue.Null];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, IExecutionThread thread)
    {
        var value = thread.Stack[0];
        AggregateFunctionsUtils.ExecuteWithNullInitialState(ref state[0], in value, VariantValue.Add);
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state) => state[0];
}
