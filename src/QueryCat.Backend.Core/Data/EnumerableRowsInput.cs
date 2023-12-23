using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Implements <see cref="IRowsInput" /> from enumerable.
/// </summary>
/// <typeparam name="TClass">Base enumerable class.</typeparam>
public class EnumerableRowsInput<TClass> : RowsInput, IDisposable where TClass : class
{
    private readonly ClassRowsFrameBuilder<TClass> _builder = new();

    protected IEnumerator<TClass>? Enumerator { get; set; }

    protected ClassRowsFrameBuilder<TClass> Builder => _builder;

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } = Array.Empty<Column>();

    public EnumerableRowsInput(IEnumerable<TClass> enumerable, Action<ClassRowsFrameBuilder<TClass>>? setup = null)
    {
        if (setup != null)
        {
            setup.Invoke(_builder);
            // ReSharper disable once VirtualMemberCallInConstructor
            Columns = _builder.Columns.ToArray();
        }

        Enumerator = enumerable.GetEnumerator();
    }

    /// <inheritdoc />
    public override void Open()
    {
    }

    /// <inheritdoc />
    public override void Close()
    {
        Enumerator?.Dispose();
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
        if (Enumerator == null)
        {
            value = VariantValue.Null;
            return ErrorCode.Error;
        }

        value = _builder.GetValue(columnIndex, Enumerator.Current);
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public override bool ReadNext()
    {
        base.ReadNext();
        if (Enumerator == null)
        {
            return false;
        }
        return Enumerator.MoveNext();
    }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Enumerator?.Dispose();
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
