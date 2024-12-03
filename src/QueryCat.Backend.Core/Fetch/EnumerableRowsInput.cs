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
    public override void Open()
    {
        Enumerator = _enumerable.GetEnumerator();
        base.Open();
    }

    /// <inheritdoc />
    public override bool ReadNext()
    {
        InitializeKeyColumns();
        base.ReadNext();
        if (Enumerator == null)
        {
            return false;
        }
        return Enumerator.MoveNext();
    }

    /// <inheritdoc />
    public override void Reset()
    {
        Close();
        Open();
        base.Reset();
    }

    /// <inheritdoc />
    public override void Close()
    {
        Enumerator?.Dispose();
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
