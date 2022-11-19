using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The class simplifies <see cref="IRowsInput" /> interface implementation.
/// </summary>
public abstract class RowsInput : IRowsInput
{
    private bool _isFirstCall = true;

    /// <summary>
    /// Query context.
    /// </summary>
    protected internal QueryContext QueryContext { get; private set; } = EmptyQueryContext.Empty;

    /// <inheritdoc />
    public abstract Column[] Columns { get; protected set; }

    /// <inheritdoc />
    public abstract void Open();

    /// <inheritdoc />
    public virtual void SetContext(QueryContext queryContext)
    {
        QueryContext = queryContext;
    }

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
    public void Explain(IndentedStringBuilder stringBuilder)
    {
    }

    /// <summary>
    /// The method is called before first ReadNext to initialize input.
    /// </summary>
    protected virtual void Load()
    {
    }
}
