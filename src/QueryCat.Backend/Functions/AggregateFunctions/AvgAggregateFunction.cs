using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Implements "average" aggregation function.
/// </summary>
[Description("Computes the average value.")]
[AggregateFunctionSignature("avg(value: integer): float")]
[AggregateFunctionSignature("avg(value: float): float")]
[AggregateFunctionSignature("avg(value: numeric): numeric")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class AvgAggregateFunction : IAggregateFunction
{
    private readonly VariantValue.BinaryFunction _addDelegate;

    public AvgAggregateFunction()
    {
        _addDelegate = VariantValue.GetAddDelegate(DataType.Integer, DataType.Integer);
    }

    /// <inheritdoc />
    public VariantValueArray GetInitialState(DataType type)
        => new(
            VariantValue.Null, // 0: sum
            new VariantValue(DataType.Integer) // 1: count
        );

    /// <inheritdoc />
    public void Invoke(VariantValueArray state, FunctionCallInfo callInfo)
    {
        var value = callInfo.GetAt(0);
        if (!value.IsNull)
        {
            AggregateFunctionsUtils.ExecuteWithNullInitialState(ref state.Values[0], in value, VariantValue.Add);
            state.Values[1] =
                _addDelegate.Invoke(in state.Values[1], in VariantValue.OneIntegerValue);
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValueArray state)
    {
        var sum = state.Values[0];
        var count = state.Values[1];

        if (count == 0)
        {
            return VariantValue.Null;
        }

        return new VariantValue(sum / (double)count.AsInteger);
    }
}
