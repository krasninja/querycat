using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Implements ROW_NUMBER aggregation function.
/// </summary>
[Description("Returns the number of the current row within its partition, counting from 1.")]
[AggregateFunctionSignature("row_number(): integer")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class RowNumberAggregateFunction : IAggregateFunction
{
    private readonly VariantValue.BinaryFunction _addDelegate;

    public RowNumberAggregateFunction()
    {
        _addDelegate = VariantValue.GetAddDelegate(DataType.Integer, DataType.Integer);
    }

    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) => new[] { VariantValue.OneIntegerValue };

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, FunctionCallInfo callInfo)
    {
        if (callInfo.WindowInfo != null)
        {
            state[0] = new(callInfo.WindowInfo.GetCurrentRowPosition() + 1);
        }
        else
        {
            state[0] = _addDelegate.Invoke(in state[0], in VariantValue.OneIntegerValue);
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state) => state[0];
}
