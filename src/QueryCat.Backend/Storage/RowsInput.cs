using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

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
    protected QueryContext QueryContext { get; private set; } = EmptyQueryContext.Empty;

    /// <inheritdoc />
    public abstract Column[] Columns { get; protected set; }

    /// <inheritdoc />
    public abstract void Open();

    /// <inheritdoc />
    public void SetContext(QueryContext queryContext)
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
            Initialize();
            _isFirstCall = false;
        }
        return OnReadNext();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _isFirstCall = true;
    }

    /// <summary>
    /// The method is called by ReadNext.
    /// </summary>
    /// <returns>True if there are remain rows to read, false if no row was read.</returns>
    protected abstract bool OnReadNext();

    /// <summary>
    /// The method is called before first ReadNext to initialize input.
    /// </summary>
    protected virtual void Initialize()
    {
    }
}
