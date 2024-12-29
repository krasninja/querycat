using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// Implements <see cref="IRowsInput" /> from enumerable.
/// </summary>
/// <typeparam name="TClass">Base enumerable class.</typeparam>
public class EnumerableRowsInput<TClass> : KeysRowsInput where TClass : class
{
    private readonly IEnumerable<TClass> _enumerable;
    private readonly ClassRowsFrameBuilder<TClass> _builder = new();

    protected ClassRowsFrameBuilder<TClass> Builder => _builder;

    protected IEnumerator<TClass>? Enumerator { get; set; }

    public EnumerableRowsInput(IEnumerable<TClass> enumerable, Action<ClassRowsFrameBuilder<TClass>>? setup = null)
    {
        _enumerable = enumerable;
        if (setup != null)
        {
            setup.Invoke(_builder);
            // ReSharper disable once VirtualMemberCallInConstructor
            Columns = _builder.Columns.ToArray();
            AddKeyColumns(_builder.KeyColumns);
        }
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (Enumerator == null)
        {
            value = VariantValue.Null;
            return ErrorCode.NoData;
        }

        value = _builder.GetValue(columnIndex, Enumerator.Current);
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        Enumerator = _enumerable.GetEnumerator();
        return base.OpenAsync(cancellationToken);
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
        return Enumerator.MoveNext();
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
        Enumerator?.Dispose();
        return Task.CompletedTask;
    }

    #region Dispose

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Enumerator?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
