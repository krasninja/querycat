using System.Collections.Concurrent;
using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Inputs;

internal abstract class BufferRowsSource : IRowsSource, IDisposable
{
    protected const int DelayMs = 12;

    private readonly IRowsSource _rowsSource;
    private Thread? _thread;
    private bool _isThreadStarted;
    private bool _isThreadFinished;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private sealed record ThreadState(BufferRowsSource BufferRowsSource);

    /// <summary>
    /// Read/write queue.
    /// </summary>
    protected ConcurrentQueue<Row> RowsQueue { get; } = new();

    /// <summary>
    /// The semaphore is used to limit number of data in the queue.
    /// </summary>
    protected SemaphoreSlim QueueCountSemaphore { get; }

    /// <summary>
    /// Buffer size.
    /// </summary>
    public int BufferSize { get; }

    protected bool EndOfData => _cancellationTokenSource.IsCancellationRequested;

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _rowsSource.QueryContext;
        set => _rowsSource.QueryContext = value;
    }

    public BufferRowsSource(IRowsSource rowsSource, int bufferSize)
    {
        _rowsSource = rowsSource;
        BufferSize = bufferSize;
        QueueCountSemaphore = new SemaphoreSlim(bufferSize);
    }

    private static async void QueueLoop(object? state)
    {
        if (state == null)
        {
            return;
        }
        var threadState = (ThreadState)state;
        var cancellationToken = threadState.BufferRowsSource._cancellationTokenSource.Token;
        var parent = threadState.BufferRowsSource;

        // Loop while we have data or cancellation requested.
        while (!cancellationToken.IsCancellationRequested)
        {
            var hasDataToProcess = await parent.CallbackAsync(cancellationToken);
            if (!hasDataToProcess)
            {
                // If we have no data - exit.
                await parent._cancellationTokenSource.CancelAsync();
                break;
            }
        }

        parent._isThreadFinished = true;
    }

    /// <summary>
    /// Callback to be called within a separate thread.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>True</c> if we have more data to process, <c>false</c> otherwise to end processing.</returns>
    protected abstract ValueTask<bool> CallbackAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public virtual async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        _thread = new Thread(QueueLoop);
        await _rowsSource.OpenAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await _rowsSource.CloseAsync(cancellationToken);
        await WaitForThreadFinishAsync(cancellationToken);
        await _cancellationTokenSource.CancelAsync();
        Dispose();
    }

    protected void StartThread()
    {
        if (_thread != null && !_isThreadStarted)
        {
            _isThreadStarted = true;
            _thread.Start(new ThreadState(this));
        }
    }

    protected async Task WaitForThreadFinishAsync(CancellationToken cancellationToken)
    {
        while (!_isThreadFinished)
        {
            await Task.Delay(DelayMs, cancellationToken);
        }
    }

    protected async Task WaitForQueueEmptyAsync(CancellationToken cancellationToken)
    {
        while (!RowsQueue.IsEmpty)
        {
            await Task.Delay(DelayMs, cancellationToken);
        }
    }

    /// <inheritdoc />
    public virtual async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _rowsSource.ResetAsync(cancellationToken);
        RowsQueue.Clear();
        QueueCountSemaphore.Release(QueueCountSemaphore.CurrentCount);
        _isThreadStarted = false;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource.Dispose();
            QueueCountSemaphore.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
