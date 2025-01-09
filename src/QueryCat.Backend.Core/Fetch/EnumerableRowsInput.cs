using System.Diagnostics.CodeAnalysis;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// Implements <see cref="IRowsInput" /> from enumerable.
/// </summary>
/// <typeparam name="TClass">Base enumerable class.</typeparam>
public abstract class EnumerableRowsInput<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TClass>
    : KeysRowsInput where TClass : class
{
    private readonly ClassRowsFrameBuilder<TClass> _builder = new();

    private IEnumerator<TClass>? _enumerator;

    private sealed class SourceEnumerableRowsInput<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
        : EnumerableRowsInput<T>
        where T : class
    {
        private readonly IEnumerable<T> _enumerable;
        private readonly Action<ClassRowsFrameBuilder<T>>? _setup;

        public SourceEnumerableRowsInput(IEnumerable<T> enumerable, Action<ClassRowsFrameBuilder<T>>? setup = null)
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
        protected override IEnumerable<T> GetData(Fetcher<T> fetcher) => _enumerable;
    }

    /// <summary>
    /// Create input from enumerable.
    /// </summary>
    /// <param name="source">Source enumerable.</param>
    /// <param name="setup">Setup action.</param>
    /// <returns>Input.</returns>
    public static EnumerableRowsInput<TClass> FromSource(IEnumerable<TClass> source, Action<ClassRowsFrameBuilder<TClass>>? setup = null)
    {
        return new SourceEnumerableRowsInput<TClass>(source, setup);
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
            value = VariantValue.Null;
            return ErrorCode.NoData;
        }

        value = _builder.GetValue(columnIndex, _enumerator.Current);
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public override async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        InitializeKeyColumns();
        await base.ReadNextAsync(cancellationToken);
        if (_enumerator == null)
        {
            return false;
        }
        return _enumerator.MoveNext();
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
        var fetcher = CreateFetcher<TClass>();
        _enumerator = GetData(fetcher).GetEnumerator();
        await base.LoadAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await CloseAsync(cancellationToken);
        await OpenAsync(cancellationToken);
        await base.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override Task CloseAsync(CancellationToken cancellationToken = default)
    {
        Dispose(true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Return the data as enumerable.
    /// </summary>
    /// <param name="fetcher">Fetcher.</param>
    /// <returns>Objects.</returns>
    protected abstract IEnumerable<TClass> GetData(Fetcher<TClass> fetcher);

    #region Dispose

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _enumerator?.Dispose();
            _enumerator = null;
        }
        base.Dispose(disposing);
    }

    #endregion
}
