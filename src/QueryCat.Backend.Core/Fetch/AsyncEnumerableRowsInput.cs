using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// Allows to create rows input from .NET objects using remote source.
/// </summary>
/// <typeparam name="TClass">Object class.</typeparam>
public abstract class AsyncEnumerableRowsInput<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TClass>
    : KeysRowsInput, IAsyncDisposable where TClass : class
{
    private readonly ClassRowsFrameBuilder<TClass> _builder = new();

    private IAsyncEnumerator<TClass>? _enumerator;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(AsyncEnumerableRowsInput<TClass>));

    private sealed class SourceAsyncEnumerableRowsInput<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
        : AsyncEnumerableRowsInput<T>
        where T : class
    {
        private readonly IAsyncEnumerable<T> _enumerable;
        private readonly Action<ClassRowsFrameBuilder<T>>? _setup;

        public SourceAsyncEnumerableRowsInput(IAsyncEnumerable<T> enumerable, Action<ClassRowsFrameBuilder<T>>? setup = null)
        {
            _enumerable = enumerable;
            _setup = setup;
        }

        /// <inheritdoc />
        protected override void Initialize(ClassRowsFrameBuilder<T> builder)
        {
            if (_setup != null)
            {
                _setup.Invoke(builder);
            }
            base.Initialize(builder);
        }

        /// <inheritdoc />
        protected override IAsyncEnumerable<T> GetDataAsync(Fetcher<T> fetcher, CancellationToken cancellationToken = default)
            => _enumerable;
    }

    /// <summary>
    /// Create input from async enumerable.
    /// </summary>
    /// <param name="source">Source enumerable.</param>
    /// <param name="setup">Setup action.</param>
    /// <returns>Input.</returns>
    public static AsyncEnumerableRowsInput<TClass> FromSource(IAsyncEnumerable<TClass> source, Action<ClassRowsFrameBuilder<TClass>>? setup = null)
    {
        return new SourceAsyncEnumerableRowsInput<TClass>(source, setup);
    }

    /// <summary>
    /// Setup columns.
    /// </summary>
    /// <param name="builder">Frame builder.</param>
    protected virtual void Initialize(ClassRowsFrameBuilder<TClass> builder)
    {
        if (builder.Columns.Count < 1)
        {
            builder.AddPublicProperties();
        }
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (_enumerator == null)
        {
            value = default;
            return ErrorCode.NotInitialized;
        }

        try
        {
            value = _builder.GetValue(columnIndex, _enumerator.Current);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Error getting value for column '{Column}' of input '{Input}': {ErrorMessage}",
                Columns[columnIndex].FullName, GetType().Name, e.Message);
            value = VariantValue.Null;
            return ErrorCode.Error;
        }
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public override async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        await base.ReadNextAsync(cancellationToken);
        if (_enumerator == null)
        {
            return false;
        }
        return await _enumerator.MoveNextAsync();
    }

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        Initialize(_builder);
        Columns = _builder.Columns.ToArray();
        AddKeyColumns(_builder.KeyColumns);
        return base.OpenAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        await base.LoadAsync(cancellationToken);

        var fetcher = CreateFetcher<TClass>();
        _enumerator = GetDataAsync(fetcher, cancellationToken).GetAsyncEnumerator(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await CloseAsync(cancellationToken);
        await OpenAsync(cancellationToken);
        await base.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await DisposeAsync();
    }

    /// <summary>
    /// Return the data as async enumerable.
    /// </summary>
    /// <param name="fetcher">Remote data fetcher.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Objects.</returns>
    protected abstract IAsyncEnumerable<TClass> GetDataAsync(
        Fetcher<TClass> fetcher,
        CancellationToken cancellationToken = default);

    #region Dispose

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_enumerator != null && _enumerator is IDisposable disposable)
            {
                disposable.Dispose();
                _enumerator = null;
            }
        }
        base.Dispose(disposing);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_enumerator != null)
        {
            await _enumerator.DisposeAsync();
            _enumerator = null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion
}
