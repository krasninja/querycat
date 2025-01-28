using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Implements ROW_NUMBER aggregation function.
/// </summary>
[SafeFunction]
[Description("Returns the number of the current row within its partition, counting from 1.")]
[AggregateFunctionSignature("row_number(\"window\"?: object<IWindowInfo>): integer")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class RowNumberAggregateFunction : IAggregateFunction
{
    private readonly VariantValue.BinaryFunction _addDelegate;

    public RowNumberAggregateFunction()
    {
        _addDelegate = VariantValue.GetAddDelegate(DataType.Integer, DataType.Integer);
    }

    /// <inheritdoc />
    public static IAggregateFunction CreateInstance() => new RowNumberAggregateFunction();

    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) => [VariantValue.OneIntegerValue];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, IExecutionThread thread)
    {
        var window = thread.Stack.Pop().As<IWindowInfo?>();
        if (window != null)
        {
            state[0] = new(window.GetCurrentRowPosition() + 1);
        }
        else
        {
            state[0] = _addDelegate.Invoke(in state[0], in VariantValue.OneIntegerValue);
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state) => state[0];
}
