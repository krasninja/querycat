using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Inputs;

/// <summary>
/// Adds delay before writing the values.
/// </summary>
internal sealed class DelayRowsOutput : IRowsOutput
{
    [SafeFunction]
    [Description("Implements delay before writing values.")]
    [FunctionSignature("delay_output(output: object<IRowsOutput>, delay_secs: integer := 5): object<IRowsOutput>")]
    public static VariantValue DelayOutput(IExecutionThread thread)
    {
        var output = thread.Stack[0].AsRequired<IRowsOutput>();
        var delaySeconds = (int)(thread.Stack[1].AsInteger ?? 5);
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
        await Task.Delay(_delay, cancellationToken);
        return await _rowsOutput.WriteValuesAsync(values, cancellationToken);
    }
}
