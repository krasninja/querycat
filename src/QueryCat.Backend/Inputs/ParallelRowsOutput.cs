using System.ComponentModel;
using Microsoft.Extensions.Logging;
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
    [Description("Allows to run output write operations in parallel. Must be used only for rows outputs that support this!")]
    [FunctionSignature("parallel_output(output: object<IRowsOutput>, max_degree?: integer): object<IRowsOutput>")]
    public static VariantValue ParallelOutput(IExecutionThread thread)
    {
        var output = thread.Stack[0].AsRequired<IRowsOutput>();
        var maxDegree = (int?)thread.Stack[1].AsInteger;
        return VariantValue.CreateFromObject(new ParallelRowsOutput(output, maxDegree));
    }

    private readonly IRowsOutput _output;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ParallelRowsOutput));

    /// <inheritdoc />
    public RowsOutputOptions Options => _output.Options;

    /// <inheritdoc />
    public ParallelRowsOutput(IRowsOutput output, int? maxDegreeOfParallelism = null) : base(output, maxDegreeOfParallelism)
    {
        _output = output;
    }

    /// <inheritdoc />
    public ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        _ = AddTask(async () =>
        {
            await _output.WriteValuesAsync(values, cancellationToken);
        }, cancellationToken);

        return ValueTask.FromResult(ErrorCode.OK);
    }
}
