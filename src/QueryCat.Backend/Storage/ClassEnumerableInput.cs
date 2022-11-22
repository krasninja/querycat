using System.Runtime.CompilerServices;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Allows to create rows input from .NET objects.
/// </summary>
/// <typeparam name="TClass">Object class.</typeparam>
public abstract class ClassEnumerableInput<TClass> :
    RowsInput, IDisposable where TClass : class
{
    private IEnumerator<TClass>? _enumerator;
    private readonly ClassRowsFrameBuilder<TClass> _builder = new();

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } = Array.Empty<Column>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public ClassEnumerableInput()
    {
    }

    /// <summary>
    /// Create rows frame.
    /// </summary>
    /// <param name="builder">Rows frame builder.</param>
    protected abstract void Initialize(ClassRowsFrameBuilder<TClass> builder);

    /// <inheritdoc />
    public override void Open()
    {
        Initialize(_builder);
        Columns = _builder.Columns.ToArray();
    }

    /// <inheritdoc />
    public override void Close()
    {
        if (_enumerator != null)
        {
            _enumerator.Dispose();
            _enumerator = null;
        }
    }

    /// <inheritdoc />
    protected override void Load()
    {
        QueryContext.ValidateAndInvokeKeyConditions();
        var fetch = new ClassEnumerableInputFetch<TClass>(this);
        _enumerator = GetData(fetch).GetEnumerator();
    }

    /// <inheritdoc />
    public override void Reset()
    {
        Close();
        base.Reset();
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (_enumerator == null)
        {
            value = VariantValue.Null;
            return ErrorCode.Error;
        }

        value = _builder.GetValue(columnIndex, _enumerator.Current);
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public override bool ReadNext()
    {
        base.ReadNext();
        if (_enumerator == null)
        {
            return false;
        }
        return _enumerator.MoveNext();
    }

    /// <summary>
    /// Get data.
    /// </summary>
    /// <param name="fetch">Fetch helper utilities.</param>
    /// <returns>Objects.</returns>
    protected virtual IEnumerable<TClass> GetData(ClassEnumerableInputFetch<TClass> fetch)
    {
        var enumerator = GetDataAsync().GetAsyncEnumerator();
        // Blocking all the way...
        try
        {
            enumerator.ConfigureAwait(false);
            while (enumerator.MoveNextAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult())
            {
                yield return enumerator.Current;
            }
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Get data async. It is a helper virtual method that is intended to simplify async overload.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Objects.</returns>
    protected virtual async IAsyncEnumerable<TClass> GetDataAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in await GetDataChunkAsync(cancellationToken))
        {
            yield return item;
        }
    }

    protected virtual Task<IEnumerable<TClass>> GetDataChunkAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<TClass>());
    }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _enumerator?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
