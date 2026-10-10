using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Inputs;

/// <summary>
/// Adds a delay after writing the values.
/// </summary>
internal sealed class DelayRowsOutput : IRowsOutput
{
    [SafeFunction]
    [Description("Implements delay before writing values.")]
    [FunctionSignature("delay_output(output: object<IRowsOutput>, delay_secs: float := 5): object<IRowsOutput>")]
    public static VariantValue DelayOutput(IExecutionThread thread)
    {
        var output = thread.Stack[0].AsRequired<IRowsOutput>();
        var delaySeconds = thread.Stack[1].AsFloat ?? 5;
        delaySeconds = Math.Clamp(delaySeconds, 0, double.MaxValue);
        return VariantValue.CreateFromObject(new DelayRowsOutput(output, TimeSpan.FromSeconds(delaySeconds)));
    }

    private readonly IRowsOutput _rowsOutput;
    private readonly TimeSpan _delay;

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _rowsOutput.QueryContext;
        set => _rowsOutput.QueryContext = value;
    }

    /// <inheritdoc />
    public RowsOutputOptions Options => _rowsOutput.Options;

    public DelayRowsOutput(IRowsOutput rowsOutput, TimeSpan delay)
    {
        _rowsOutput = rowsOutput;
        _delay = delay;
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) => _rowsOutput.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) => _rowsOutput.CloseAsync(cancellationToken);

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) => _rowsOutput.ResetAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        var errorCode = await _rowsOutput.WriteValuesAsync(values, cancellationToken);
        await Task.Delay(_delay, cancellationToken);
        return errorCode;
    }
}
