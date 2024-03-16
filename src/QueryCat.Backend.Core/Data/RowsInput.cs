using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// The class simplifies <see cref="IRowsInput" /> interface implementation.
/// </summary>
public abstract class RowsInput : IRowsInput
{
    private bool _isFirstCall = true;

    /// <summary>
    /// Query context.
    /// </summary>
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    /// <inheritdoc />
    public abstract Column[] Columns { get; protected set; }

    /// <inheritdoc />
    public virtual string[] UniqueKey { get; protected set; } = Array.Empty<string>();

    /// <inheritdoc />
    public abstract void Open();

    /// <inheritdoc />
    public abstract void Close();

    /// <inheritdoc />
    public abstract ErrorCode ReadValue(int columnIndex, out VariantValue value);

    /// <inheritdoc />
    public virtual bool ReadNext()
    {
        if (_isFirstCall)
        {
            Load();
            _isFirstCall = false;
        }
        return true;
    }

    /// <inheritdoc />
    public virtual void Reset()
    {
        _isFirstCall = true;
    }

    /// <inheritdoc />
    public virtual void Explain(IndentedStringBuilder stringBuilder)
    {
    }

    /// <summary>
    /// The method is called before first ReadNext to initialize input.
    /// </summary>
    protected virtual void Load()
    {
    }
}
