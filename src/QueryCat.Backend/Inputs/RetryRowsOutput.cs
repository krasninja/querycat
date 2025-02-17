using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Inputs;

/// <summary>
/// Implements retry resilience strategy with constant delay interval for rows output.
/// </summary>
internal sealed class RetryRowsOutput : RetryRowsSource, IRowsOutput
{
    [SafeFunction]
    [Description("Implements retry resilience strategy with constant delay interval for rows output.")]
    [FunctionSignature("retry_output(output: object<IRowsOutput>, max_attempts: integer := 3, retry_interval_secs: float := 5.0): object<IRowsOutput>")]
    public static VariantValue RetryOutput(IExecutionThread thread)
    {
        var output = thread.Stack[0].AsRequired<IRowsOutput>();
        var maxAttempts = (int)(thread.Stack[1].AsInteger ?? 3);
        var retryIntervalSecs = thread.Stack[2].AsFloat ?? 5.0;
        return VariantValue.CreateFromObject(
            new RetryRowsOutput(output, maxAttempts, TimeSpan.FromSeconds(retryIntervalSecs)));
    }

    private readonly IRowsOutput _rowsOutput;

    /// <inheritdoc />
    public RowsOutputOptions Options => _rowsOutput.Options;

    public RetryRowsOutput(IRowsOutput rowsOutput, int maxAttempts = 3, TimeSpan? retryInterval = null)
        : base(rowsOutput, maxAttempts, retryInterval)
    {
        _rowsOutput = rowsOutput;
    }

    /// <inheritdoc />
    public ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        return RetryWrapperAsync(async (localValues, ct)
            => await _rowsOutput.WriteValuesAsync(localValues, ct), values, cancellationToken);
    }
}
