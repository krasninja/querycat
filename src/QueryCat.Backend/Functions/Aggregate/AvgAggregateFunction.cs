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
            VariantValue.Null, // 0: sum (for timestamp: the first value)
            new(DataType.Integer), // 1: count
            VariantValue.Null, // 2: timestamp only: sum of offsets from the first value
        ];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, IExecutionThread thread)
    {
        var value = thread.Stack[0];
        if (value.IsNull)
        {
            return;
        }

        if (value.Type == DataType.Timestamp)
        {
            if (state[0].IsNull)
            {
                state[0] = value;
                state[2] = new VariantValue(TimeSpan.Zero);
            }
            else
            {
                var offset = value.AsTimestamp!.Value - state[0].AsTimestamp!.Value;
                state[2] = new VariantValue(state[2].AsInterval!.Value + offset);
            }
        }
        else
        {
            AggregateFunctionsUtils.ExecuteWithNullInitialState(ref state[0], in value, VariantValue.Add);
        }
        state[1] = _addDelegate.Invoke(in state[1], in VariantValue.OneIntegerValue);
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state)
    {
        var sum = state[0];
        var count = state[1].AsInteger ?? 0;
        if (sum.IsNull || count == 0)
        {
            return VariantValue.Null;
        }

        return sum.Type switch
        {
            DataType.Integer or DataType.Float => new VariantValue(sum.AsFloat!.Value / count),
            DataType.Numeric => new VariantValue(sum.AsNumeric!.Value / count),
            DataType.Interval => new VariantValue(sum.AsInterval!.Value / count),
            DataType.Timestamp => new VariantValue(sum.AsTimestamp!.Value + state[2].AsInterval!.Value / count),
            _ => VariantValue.Null,
        };
    }
}
