using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Inputs;

internal class ParallelRowsSource : IRowsSource, IDisposable, IAsyncDisposable
{
    private readonly IRowsSource _source;
    private volatile bool _isDisposed;
    private long _runningTasksCount;
    private Exception? _firstException;
    private readonly int _maxDegreeOfParallelism;
    private CancellationTokenSource _workersCancellationTokenSource = new();

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
        _maxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount;
        ArgumentOutOfRangeException.ThrowIfLessThan(_maxDegreeOfParallelism, 1, nameof(maxDegreeOfParallelism));
        ParallelSemaphore = new SemaphoreSlim(_maxDegreeOfParallelism, _maxDegreeOfParallelism);
    }

    protected async ValueTask AddTaskAsync(Func<CancellationToken, ValueTask<ErrorCode>> func, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        cancellationToken.ThrowIfCancellationRequested();
        await ParallelSemaphore.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
        Interlocked.Increment(ref _runningTasksCount);
        var ct = _workersCancellationTokenSource.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await func.Invoke(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Aborted.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Parallel task failed.");
                Interlocked.CompareExchange(ref _firstException, ex, null);
            }
            finally
            {
                Interlocked.Decrement(ref _runningTasksCount);
                ParallelSemaphore.Release();
            }
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) => _source.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await WaitForAllPendingTasksAsync(cancellationToken).ConfigureAwait(false);
            ThrowIfFaulted(clear: true);
        }
        finally
        {
            await _source.CloseAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await WaitForAllPendingTasksAsync(cancellationToken)
                .ConfigureAwait(false);
            _runningTasksCount = 0;
            ThrowIfFaulted(clear: true);

            // If cancellation requested - renew CancellationTokenSource.
            if (_workersCancellationTokenSource.IsCancellationRequested)
            {
                _workersCancellationTokenSource.Dispose();
                _workersCancellationTokenSource = new CancellationTokenSource();
            }
        }
        finally
        {
            await _source.ResetAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async ValueTask WaitForAllPendingTasksAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pending tasks {PendingTasksCount}.", Interlocked.Read(ref _runningTasksCount));

        var acquired = 0;
        try
        {
            for (; acquired < _maxDegreeOfParallelism; acquired++)
            {
                await ParallelSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            await _workersCancellationTokenSource.CancelAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (acquired > 0)
            {
                ParallelSemaphore.Release(acquired);
            }
        }
    }

    private void ThrowIfFaulted(bool clear)
    {
        var exception = clear
            ? Interlocked.Exchange(ref _firstException, null)
            : Volatile.Read(ref _firstException);
        if (exception != null)
        {
            ExceptionDispatchInfo.Throw(exception);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        if (disposing)
        {
            _workersCancellationTokenSource.Cancel();
            var allReleased = true;
            for (var i = 0; i < _maxDegreeOfParallelism && allReleased; i++)
            {
                allReleased = ParallelSemaphore.Wait(TimeSpan.FromSeconds(5));
            }
            _workersCancellationTokenSource.Dispose();
            if (allReleased)
            {
                ParallelSemaphore.Dispose();
            }
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
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        await _workersCancellationTokenSource.CancelAsync().ConfigureAwait(false);
        await WaitForAllPendingTasksAsync().ConfigureAwait(false);
        _workersCancellationTokenSource.Dispose();
        ParallelSemaphore.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
