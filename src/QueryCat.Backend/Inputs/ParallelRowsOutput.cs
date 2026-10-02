using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Inputs;

/// <summary>
/// Allows to run output write operations in parallel.
/// </summary>
internal sealed class ParallelRowsOutput : ParallelRowsSource, IRowsOutput
{
    [SafeFunction]
    [Description("Allows to run output write operations in parallel. Must be used only for rows outputs that support this!")]
    [FunctionSignature("parallel_output(output: object<IRowsOutput>, max_degree?: integer): object<IRowsOutput>")]
    public static VariantValue ParallelOutput(IExecutionThread thread)
    {
        var output = thread.Stack[0].AsRequired<IRowsOutput>();
        var maxDegree = (int?)thread.Stack[1].AsInteger;
        if (maxDegree < 1)
        {
            maxDegree = 1;
        }
        return VariantValue.CreateFromObject(new ParallelRowsOutput(output, maxDegree));
    }

    private readonly IRowsOutput _output;

    /// <inheritdoc />
    public RowsOutputOptions Options => _output.Options;

    /// <inheritdoc />
    public ParallelRowsOutput(IRowsOutput output, int? maxDegreeOfParallelism = null) : base(output, maxDegreeOfParallelism)
    {
        _output = output;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        var localValues = new VariantValue[values.Length];
        Array.Copy(values, localValues, values.Length);
        await AddTaskAsync(ct => _output.WriteValuesAsync(localValues, ct), cancellationToken)
            .ConfigureAwait(false);
        return ErrorCode.OK;
    }
}
