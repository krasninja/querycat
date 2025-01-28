using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Implements "count" aggregation function.
/// </summary>
[SafeFunction]
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
    public static IAggregateFunction CreateInstance() => new CountAggregateFunction();

    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) => [new(0)];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, IExecutionThread thread)
    {
        if (!thread.Stack[0].IsNull)
        {
            state[0] =
                _addDelegate.Invoke(in state[0], in VariantValue.OneIntegerValue);
        }
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state) => state[0];
}
