using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The class simplifies <see cref="IRowsOutput" /> implementation.
/// </summary>
public abstract class RowsOutput : IRowsOutput
{
    private bool _isFirstCall = true;

    /// <summary>
    /// Query context.
    /// </summary>
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    /// <inheritdoc />
    public abstract void Open();

    /// <inheritdoc />
    public abstract void Close();

    /// <inheritdoc />
    public void Reset()
    {
        _isFirstCall = true;
    }

    /// <inheritdoc />
    public RowsOutputOptions Options { get; protected set; } = new();

    /// <inheritdoc />
    public ErrorCode WriteValues(VariantValue[] values)
    {
        if (_isFirstCall)
        {
            Initialize();
            _isFirstCall = false;
        }
        OnWrite(values);
        return ErrorCode.OK;
    }

    /// <summary>
    /// Write a row.
    /// </summary>
    /// <param name="values">Values to write.</param>
    protected abstract void OnWrite(in VariantValue[] values);

    /// <summary>
    /// The method is called before first Write to initialize input.
    /// </summary>
    protected virtual void Initialize()
    {
    }
}
