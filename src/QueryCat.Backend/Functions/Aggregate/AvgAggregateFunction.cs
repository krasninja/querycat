using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Implements "average" aggregation function.
/// </summary>
[SafeFunction]
[Description("Computes the average value.")]
[AggregateFunctionSignature("avg(value: integer): float")]
[AggregateFunctionSignature("avg(value: float): float")]
[AggregateFunctionSignature("avg(value: numeric): numeric")]
[AggregateFunctionSignature("avg(value: timestamp): timestamp")]
[AggregateFunctionSignature("avg(value: interval): interval")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class AvgAggregateFunction : IAggregateFunction
{
    private readonly VariantValue.BinaryFunction _addDelegate;

    public AvgAggregateFunction()
    {
        _addDelegate = VariantValue.GetAddDelegate(DataType.Integer, DataType.Integer);
    }

    /// <inheritdoc />
    public static IAggregateFunction CreateInstance() => new AvgAggregateFunction();

    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type)
        =>
        [
            VariantValue.Null, // 0: sum
            new(DataType.Integer) // 1: count
        ];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, IExecutionThread thread)
    {
        var value = thread.Stack[0];
        if (!value.IsNull)
        {
            AggregateFunctionsUtils.ExecuteWithNullInitialState(ref state[0], in value, VariantValue.Add);
            state[1] =
                _addDelegate.Invoke(in state[1], in VariantValue.OneIntegerValue);
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state)
    {
        var sum = state[0];
        var count = state[1];

        if (count == 0 || !count.AsInteger.HasValue)
        {
            return VariantValue.Null;
        }

        return new VariantValue(sum / (double)count.AsInteger);
    }
}
