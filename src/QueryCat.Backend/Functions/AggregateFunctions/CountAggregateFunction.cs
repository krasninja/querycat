using System.ComponentModel;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Implements "count" aggregation function.
/// </summary>
[Description("Computes the number of input rows.")]
[AggregateFunctionSignature("count(value: any): integer")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class CountAggregateFunction : IAggregateFunction
{
    private readonly VariantValue.BinaryFunction _addDelegate;

    public CountAggregateFunction()
    {
        _addDelegate = VariantValue.GetAddDelegate(DataType.Integer, DataType.Integer);
    }

    /// <inheritdoc />
    public VariantValueArray GetInitialState(DataType type)
        => new(new VariantValue(0));

    /// <inheritdoc />
    public void Invoke(VariantValueArray state, FunctionCallInfo callInfo)
    {
        if (!callInfo.GetAt(0).IsNull)
        {
            state.Values[0] =
                _addDelegate.Invoke(in state.Values[0], in VariantValue.OneIntegerValue);
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValueArray state) => state.Values[0];
}
