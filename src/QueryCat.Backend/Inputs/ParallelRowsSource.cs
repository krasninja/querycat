using System.Collections.Concurrent;
using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Inputs;

internal class ParallelRowsSource : IRowsSource, IDisposable, IAsyncDisposable
{
    private readonly IRowsSource _source;
    private bool _isDisposed;

    protected SemaphoreSlim Semaphore { get; }

    protected ConcurrentDictionary<int, Task> Tasks { get; } = new();

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

    protected async ValueTask AddTask(Func<ValueTask> func, CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken);
        var task = new Task(() => func(), cancellationToken);
#pragma warning disable CS4014
        task.ContinueWith(t =>
#pragma warning restore CS4014
        {
            Semaphore.Release();
            Tasks.TryRemove(task.Id, out _);
        }, cancellationToken);
        Tasks.TryAdd(task.Id, task);
        task.Start();
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
        foreach (var task in Tasks.Values)
        {
            await task.ConfigureAwait(false);
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
