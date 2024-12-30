namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// Allows to create rows input from .NET objects using remote source.
/// </summary>
/// <typeparam name="TClass">Object class.</typeparam>
public abstract class FetchRowsInput<TClass> : KeysRowsInput where TClass : class
{
    private readonly ClassRowsFrameBuilder<TClass> _builder = new();

    private IAsyncEnumerator<TClass>? Enumerator { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public FetchRowsInput(Action<ClassRowsFrameBuilder<TClass>>? setup = null)
    {
        if (setup != null)
        {
            setup.Invoke(_builder);
            // ReSharper disable once VirtualMemberCallInConstructor
            Columns = _builder.Columns.ToArray();
            AddKeyColumns(_builder.KeyColumns);
        }
    }

    /// <summary>
    /// Create rows frame.
    /// </summary>
    /// <param name="builder">Rows frame builder.</param>
    protected abstract void Initialize(ClassRowsFrameBuilder<TClass> builder);

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        Initialize(_builder);
        Columns = _builder.Columns.ToArray();
        AddKeyColumns(_builder.KeyColumns);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        InitializeKeyColumns();
        await base.ReadNextAsync(cancellationToken);
        if (Enumerator == null)
        {
            return false;
        }
        return await Enumerator.MoveNextAsync();
    }

    /// <inheritdoc />
    public override async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (Enumerator != null)
        {
            await Enumerator.DisposeAsync();
        }
    }

    /// <inheritdoc />
    protected override async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        await base.LoadAsync(cancellationToken);

        var fetch = new Fetcher<TClass>();
        var queryLimit = QueryContext.QueryInfo.Limit + QueryContext.QueryInfo.Offset;
        if (queryLimit.HasValue && AreAllKeyColumnsSet)
        {
            fetch.Limit = Math.Min((int)queryLimit.Value, fetch.Limit);
        }
        Enumerator = GetDataAsync(fetch, cancellationToken).GetAsyncEnumerator(cancellationToken);
    }

    /// <summary>
    /// Get data.
    /// </summary>
    /// <param name="fetcher">Remote data fetcher.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Objects.</returns>
    protected abstract IAsyncEnumerable<TClass> GetDataAsync(
        Fetcher<TClass> fetcher,
        CancellationToken cancellationToken = default);
}
