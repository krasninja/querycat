using System.Collections.Concurrent;
using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Inputs;

internal abstract class BufferRowsSource : IRowsSource, IDisposable
{
    private readonly IRowsSource _source;
    private Thread? _thread;
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
    /// The semaphore is used to sync queue read and write in case of empty queue.
    /// </summary>
    protected SemaphoreSlim WriteSemaphore { get; } = new(1);

    /// <summary>
    /// Buffer size.
    /// </summary>
    public int BufferSize { get; }

    protected bool EndOfData => _cancellationTokenSource.IsCancellationRequested;

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _source.QueryContext;
        set => _source.QueryContext = value;
    }

    public BufferRowsSource(IRowsSource source, int bufferSize)
    {
        _source = source;
        BufferSize = bufferSize;
        QueueCountSemaphore = new SemaphoreSlim(bufferSize);
    }

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        _thread = new Thread(QueueLoop);
        await _source.OpenAsync(cancellationToken);
        _thread.Start(new ThreadState(this));
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
            // Seems we don't need to use write/read sync if we have enough data.
            var shouldUseWriteSemaphore = parent.RowsQueue.Count > 3;
            try
            {
                await parent.QueueCountSemaphore.WaitAsync(cancellationToken);
                if (shouldUseWriteSemaphore)
                {
                    await parent.WriteSemaphore.WaitAsync(cancellationToken);
                }
                var row = await parent.CallbackAsync(cancellationToken);
                if (row != null)
                {
                    // If we have data - enqueue.
                    parent.RowsQueue.Enqueue(row);
                }
                else
                {
                    // If we have no data - exit.
                    await parent._cancellationTokenSource.CancelAsync();
                }
            }
            finally
            {
                if (shouldUseWriteSemaphore)
                {
                    parent.WriteSemaphore.Release();
                }
            }
        }
    }

    protected abstract ValueTask<Row?> CallbackAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await _source.CloseAsync(cancellationToken);
        Dispose();
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _source.ResetAsync(cancellationToken);
        RowsQueue.Clear();
        WriteSemaphore.Release(WriteSemaphore.CurrentCount);
        QueueCountSemaphore.Release(QueueCountSemaphore.CurrentCount);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
        QueueCountSemaphore.Dispose();
        WriteSemaphore.Dispose();
    }
}
