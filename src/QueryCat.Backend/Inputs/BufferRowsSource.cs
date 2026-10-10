using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Inputs;

/// <summary>
/// Base class for buffered rows sources. Rows are passed between the caller and the wrapped
/// source through a bounded channel; the other side of the channel is served by a background worker.
/// </summary>
internal abstract class BufferRowsSource : IRowsSource, IAsyncDisposable
{
    public const int DefaultBufferSize = 1024;

    private readonly IRowsSource _rowsSource;
    private Channel<VariantValue[]>? _channel;
    private CancellationTokenSource? _workerCancellationTokenSource;
    private Task? _workerTask;
    private ExceptionDispatchInfo? _workerError;

    /// <summary>
    /// Buffer size.
    /// </summary>
    public int BufferSize { get; }

    /// <summary>
    /// <c>True</c> if the background worker has been started and not stopped yet.
    /// </summary>
    protected bool IsWorkerStarted => _workerTask != null;

    /// <summary>
    /// <c>True</c> to let the worker process all buffered rows on close/reset and rethrow its error (output),
    /// <c>false</c> to cancel the worker and discard buffered rows (input).
    /// </summary>
    protected abstract bool FlushOnStop { get; }

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _rowsSource.QueryContext;
        set => _rowsSource.QueryContext = value;
    }

    protected BufferRowsSource(IRowsSource rowsSource, int bufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, 1);
        _rowsSource = rowsSource;
        BufferSize = bufferSize;
    }

    /// <summary>
    /// Get and validate buffer size function argument.
    /// </summary>
    /// <param name="value">Argument value.</param>
    /// <returns>Buffer size.</returns>
    protected static int GetBufferSize(VariantValue value)
    {
        var size = value.AsInteger ?? DefaultBufferSize;
        if (size < 1 || size > int.MaxValue)
        {
            throw new QueryCatException(string.Format(Resources.Errors.InvalidBufferSize, size));
        }
        return (int)size;
    }

    /// <summary>
    /// The worker body. The input implementation writes into the channel, the output one reads from it.
    /// Channel completion is handled by the base class.
    /// </summary>
    /// <param name="channel">Rows channel.</param>
    /// <param name="cancellationToken">Cancellation token, triggered on reset/close.</param>
    /// <returns>Awaitable task.</returns>
    protected abstract Task RunWorkerAsync(Channel<VariantValue[]> channel, CancellationToken cancellationToken);

    /// <summary>
    /// Start the worker if it is not running and return the rows channel.
    /// </summary>
    /// <returns>Rows channel.</returns>
    protected Channel<VariantValue[]> EnsureWorkerStarted()
    {
        if (_channel != null)
        {
            return _channel;
        }

        var channel = Channel.CreateBounded<VariantValue[]>(new BoundedChannelOptions(BufferSize)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
        _workerCancellationTokenSource = new CancellationTokenSource();
        var token = _workerCancellationTokenSource.Token;
        // Do not pass the token to Task.Run: the core method must always run to complete the channel.
        _workerTask = Task.Run(() => RunWorkerCoreAsync(channel, token), token);
        _channel = channel;
        return channel;
    }

    /// <summary>
    /// Rethrow the worker exception (with the original stack trace) if any, only once.
    /// </summary>
    protected void ThrowIfWorkerFailed()
    {
        var error = _workerError;
        if (error != null)
        {
            _workerError = null;
            error.Throw();
        }
    }

    private async Task RunWorkerCoreAsync(Channel<VariantValue[]> channel, CancellationToken cancellationToken)
    {
        try
        {
            await RunWorkerAsync(channel, cancellationToken);
            channel.Writer.TryComplete();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Stopped by reset/close.
            channel.Writer.TryComplete();
        }
        catch (Exception ex)
        {
            // Never let the exception escape the worker: store it and pass it to the other side.
            _workerError = ExceptionDispatchInfo.Capture(ex);
            channel.Writer.TryComplete(ex);
        }
    }

    private async Task<ExceptionDispatchInfo?> StopWorkerAsync(bool flush, CancellationToken cancellationToken)
    {
        if (_workerTask == null)
        {
            return null;
        }

        var workerCancellationTokenSource = _workerCancellationTokenSource!;
        try
        {
            if (flush)
            {
                // Let the worker drain the buffer; caller cancellation aborts the drain.
                _channel!.Writer.TryComplete();
                await using (cancellationToken.Register(
                    static state => ((CancellationTokenSource)state!).Cancel(), workerCancellationTokenSource))
                {
                    await _workerTask;
                }
            }
            else
            {
                // Cancel first, then wait: the worker may be blocked on a full channel.
                await workerCancellationTokenSource.CancelAsync();
                await _workerTask;
            }
            return _workerError;
        }
        finally
        {
            workerCancellationTokenSource.Dispose();
            _workerCancellationTokenSource = null;
            _workerTask = null;
            _channel = null;
            _workerError = null;
        }
    }

    private async Task StopAsync(CancellationToken cancellationToken)
    {
        var error = await StopWorkerAsync(FlushOnStop, cancellationToken);
        if (FlushOnStop)
        {
            error?.Throw();
        }
    }

    /// <inheritdoc />
    public virtual Task OpenAsync(CancellationToken cancellationToken = default)
        => _rowsSource.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await StopAsync(cancellationToken);
        }
        finally
        {
            await _rowsSource.CloseAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public virtual async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        // The worker must be stopped before the source is touched from this thread.
        await StopAsync(cancellationToken);
        await _rowsSource.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopWorkerAsync(flush: false, CancellationToken.None);
        GC.SuppressFinalize(this);
    }
}
