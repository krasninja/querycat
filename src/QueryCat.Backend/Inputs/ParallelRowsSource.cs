using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Inputs;

internal class ParallelRowsSource : IRowsSource, IDisposable, IAsyncDisposable
{
    private readonly IRowsSource _source;
    private bool _isDisposed;
    private long _runningTasksCount;

    protected SemaphoreSlim ParallelSemaphore { get; }

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ParallelRowsSource));

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _source.QueryContext;
        set => _source.QueryContext = value;
    }

    public ParallelRowsSource(IRowsSource source, int? maxDegreeOfParallelism = null)
    {
        _source = source;
        ParallelSemaphore = new SemaphoreSlim(maxDegreeOfParallelism ?? Environment.ProcessorCount);
    }

    protected async ValueTask AddTask(Func<CancellationToken, Task> func, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ParallelSemaphore.WaitAsync(cancellationToken);
        Interlocked.Increment(ref _runningTasksCount);
        _ = Task.Factory.StartNew(async () =>
        {
            try
            {
                await func.Invoke(CancellationToken.None);
            }
            finally
            {
                ParallelSemaphore.Release();
                Interlocked.Decrement(ref _runningTasksCount);
            }
        }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) => _source.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await WaitForAllPendingTasksAsync(cancellationToken).ConfigureAwait(false);
        await _source.CloseAsync(cancellationToken).ConfigureAwait(false);
        await DisposeAsyncCore().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await WaitForAllPendingTasksAsync(cancellationToken)
            .ConfigureAwait(false);
        _runningTasksCount = 0;
        await _source.ResetAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask WaitForAllPendingTasksAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pending tasks {PendingTasksCount}.", Interlocked.Read(ref _runningTasksCount));
        while (Interlocked.Read(ref _runningTasksCount) > 0)
        {
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            ParallelSemaphore.Wait(TimeSpan.FromSeconds(5));
            ParallelSemaphore.Dispose();
            _isDisposed = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        await WaitForAllPendingTasksAsync();

        if (_isDisposed)
        {
            return;
        }
        await ParallelSemaphore.WaitAsync(TimeSpan.FromSeconds(5))
            .ConfigureAwait(false);
        ParallelSemaphore.Dispose();
        _isDisposed = true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
}
