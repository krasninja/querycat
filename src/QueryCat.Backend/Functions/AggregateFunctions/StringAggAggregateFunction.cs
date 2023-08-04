using System.ComponentModel;
using System.Text;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Implements string_agg aggregation function.
/// </summary>
[Description("Concatenates the non-null input values into a string.")]
[AggregateFunctionSignature("string_agg(target: string, delimiter: string): string")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class StringAggAggregateFunction : IAggregateFunction
{
    /// <inheritdoc />
    public VariantValueArray GetInitialState(DataType type) =>
        new(VariantValue.CreateFromObject(new StringBuilder()));

    /// <inheritdoc />
    public void Invoke(VariantValueArray state, FunctionCallInfo callInfo)
    {
        var target = callInfo.GetAt(0);
        var delimiter = callInfo.GetAt(1).AsString;

        if (target.IsNull)
        {
            return;
        }

        var sb = state.Values[0].As<StringBuilder>();
        if (sb.Length > 0)
        {
            sb.Append(delimiter);
        }
        sb.Append(target.AsString);
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValueArray state)
        => new(state.Values[0].As<StringBuilder>().ToString());
}
