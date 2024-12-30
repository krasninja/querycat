using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// Implements <see cref="IRowsInput" /> from enumerable.
/// </summary>
/// <typeparam name="TClass">Base enumerable class.</typeparam>
public class EnumerableRowsInput<TClass> : KeysRowsInput where TClass : class
{
    private readonly ClassRowsFrameBuilder<TClass> _builder = new();

    private readonly IEnumerable<TClass>? _enumerable;
    private IEnumerator<TClass>? _enumerator;

    /// <summary>
    /// Constructor. Use GetData() method to iteration.
    /// </summary>
    /// <param name="setup">Setup action.</param>
    public EnumerableRowsInput(Action<ClassRowsFrameBuilder<TClass>>? setup = null)
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
    /// Constructor. Use the instance of <see cref="IEnumerable{TClass}" /> for iteration.
    /// </summary>
    /// <param name="enumerable">Enumerable to iterate over.</param>
    /// <param name="setup">Setup action.</param>
    public EnumerableRowsInput(IEnumerable<TClass> enumerable, Action<ClassRowsFrameBuilder<TClass>>? setup = null)
        : this(setup)
    {
        _enumerable = enumerable;
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
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_enumerable != null)
        {
            _enumerator = _enumerable.GetEnumerator();
        }
        else
        {
            _enumerator = GetData().GetEnumerator();
        }
        return base.OpenAsync(cancellationToken);
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
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await CloseAsync(cancellationToken);
        await OpenAsync(cancellationToken);
        await base.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _enumerator?.Dispose();
        return Task.CompletedTask;
    }

    protected virtual IEnumerable<TClass> GetData() => [];

    #region Dispose

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _enumerator?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
