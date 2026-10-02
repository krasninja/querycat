using System.ComponentModel;
using System.Threading.Channels;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Inputs;

internal sealed class BufferRowsOutput : BufferRowsSource, IRowsOutput
{
    [SafeFunction]
    [Description("Implements buffer for rows output.")]
    [FunctionSignature("buffer_output(output: object<IRowsOutput>, size: integer := 1024): object<IRowsOutput>")]
    public static VariantValue BufferOutput(IExecutionThread thread)
    {
        var output = thread.Stack[0].AsRequired<IRowsOutput>();
        var bufferSize = GetBufferSize(thread.Stack[1]);
        return VariantValue.CreateFromObject(new BufferRowsOutput(output, bufferSize));
    }

    private readonly IRowsOutput _rowsOutput;

    /// <inheritdoc />
    protected override bool FlushOnStop => true;

    /// <inheritdoc />
    public RowsOutputOptions Options => _rowsOutput.Options;

    public BufferRowsOutput(IRowsOutput rowsOutput, int bufferSize) : base(rowsOutput, bufferSize)
    {
        _rowsOutput = rowsOutput;
    }

    /// <inheritdoc />
    protected override async Task RunWorkerAsync(Channel<VariantValue[]> channel, CancellationToken cancellationToken)
    {
        var reader = channel.Reader;
        while (await reader.WaitToReadAsync(cancellationToken))
        {
            while (reader.TryRead(out var values))
            {
                await _rowsOutput.WriteValuesAsync(values, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        var channel = EnsureWorkerStarted();
        try
        {.
            await channel.Writer.WriteAsync(values.ToArray(), cancellationToken);
        }
        catch (ChannelClosedException)
        {
            ThrowIfWorkerFailed();
            throw;
        }

        return ErrorCode.OK;
    }
}
