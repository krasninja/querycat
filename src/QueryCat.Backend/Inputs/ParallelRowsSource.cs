using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Inputs;

internal class ParallelRowsSource : IRowsSource, IDisposable, IAsyncDisposable
{
    private readonly IRowsSource _source;
    private bool _isDisposed;

    protected SemaphoreSlim Semaphore { get; }

    protected ConcurrentDictionary<int, Task> Tasks { get; } = new();

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
        Semaphore = new SemaphoreSlim(maxDegreeOfParallelism ?? Environment.ProcessorCount);
    }

    protected async ValueTask AddTask(Func<Task> func, CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken);
        var task = func.Invoke();
        Tasks.TryAdd(task.Id, task);
#pragma warning disable CS4014
        task.ContinueWith(t =>
#pragma warning restore CS4014
        {
            Semaphore.Release();
            Tasks.TryRemove(task.Id, out _);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) => _source.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await DisposeAsyncCore();
        await _source.CloseAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) => _source.ResetAsync(cancellationToken);

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            Semaphore.Wait(TimeSpan.FromSeconds(5));
            Semaphore.Dispose();
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
        _logger.LogDebug("Closing parallel source, pending tasks {PendingTasksCount}.", Tasks.Count);
        foreach (var task in Tasks.Values)
        {
            await task;
        }
        Tasks.Clear();

        if (_isDisposed)
        {
            return;
        }
        await Semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Semaphore.Dispose();
        _isDisposed = true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
}
