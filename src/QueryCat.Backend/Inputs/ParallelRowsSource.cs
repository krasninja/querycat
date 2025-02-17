using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Inputs;

internal class ParallelRowsSource : IRowsSource, IDisposable, IAsyncDisposable
{
    private readonly IRowsSource _source;

    protected SemaphoreSlim Semaphore { get; }

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

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) => _source.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) => _source.CloseAsync(cancellationToken);

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) => _source.ResetAsync(cancellationToken);

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Semaphore.Wait(TimeSpan.FromSeconds(5));
            Semaphore.Dispose();
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
        await Semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Semaphore.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
}
