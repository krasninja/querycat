using System.ComponentModel;
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
        var bufferSize = (int)(thread.Stack[1].AsInteger ?? 1024);
        return VariantValue.CreateFromObject(new BufferRowsOutput(output, bufferSize));
    }

    private readonly IRowsOutput _rowsSource;
    private bool _isEndOfData;

    /// <inheritdoc />
    public RowsOutputOptions Options => _rowsSource.Options;

    /// <inheritdoc />
    public BufferRowsOutput(IRowsOutput rowsSource, int bufferSize) : base(rowsSource, bufferSize)
    {
        _rowsSource = rowsSource;
    }

    /// <inheritdoc />
    protected override async ValueTask<bool> CallbackAsync(CancellationToken cancellationToken)
    {
        if (_isEndOfData)
        {
            return false;
        }

        if (RowsQueue.TryDequeue(out var row))
        {
            await _rowsSource.WriteValuesAsync(row.AsArray(copy: false), cancellationToken);
        }
        else
        {
            await Task.Delay(DelayMs, cancellationToken);
        }

        return true;
    }

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await WaitForQueueEmptyAsync(cancellationToken);
        _isEndOfData = false;
        await base.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await WaitForQueueEmptyAsync(cancellationToken);
        _isEndOfData = true;
        await base.CloseAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        StartThread();
        await QueueCountSemaphore.WaitAsync(cancellationToken);

        try
        {
            var row = new Row(_rowsSource.QueryContext.QueryInfo.Columns);
            Row.Copy(values, row);
            RowsQueue.Enqueue(row);
        }
        finally
        {
            QueueCountSemaphore.Release();
        }

        return ErrorCode.OK;
    }
}
