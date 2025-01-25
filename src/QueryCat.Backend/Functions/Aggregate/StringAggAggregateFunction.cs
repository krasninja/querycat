using System.ComponentModel;
using System.Text;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Implements string_agg aggregation function.
/// </summary>
[SafeFunction]
[Description("Concatenates the non-null input values into a string.")]
[AggregateFunctionSignature("string_agg(target: string, delimiter: string): string")]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class StringAggAggregateFunction : IAggregateFunction
{
    /// <inheritdoc />
    public static IAggregateFunction CreateInstance() => new StringAggAggregateFunction();

    /// <inheritdoc />
    public VariantValue[] GetInitialState(DataType type) =>
        [VariantValue.CreateFromObject(new StringBuilder())];

    /// <inheritdoc />
    public void Invoke(VariantValue[] state, IExecutionThread thread)
    {
        var target = thread.Stack[0];
        var delimiter = thread.Stack[1].AsString;

        if (target.IsNull)
        {
            return;
        }

        var sb = state[0].AsRequired<StringBuilder>();
        if (sb.Length > 0)
        {
            sb.Append(delimiter);
        }
        sb.Append(target.AsString);
    }

    /// <inheritdoc />
    public VariantValue GetResult(VariantValue[] state) => new(state[0].AsRequired<StringBuilder>().ToString());
}
