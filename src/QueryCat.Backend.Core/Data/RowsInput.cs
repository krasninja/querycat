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
    public virtual QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    /// <inheritdoc />
    public abstract Column[] Columns { get; protected set; }

    /// <inheritdoc />
    public virtual string[] UniqueKey { get; protected set; } = [];

    /// <inheritdoc />
    public abstract Task OpenAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task CloseAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ErrorCode ReadValue(int columnIndex, out VariantValue value);

    public virtual async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        if (_isFirstCall)
        {
            await LoadAsync(cancellationToken);
            _isFirstCall = false;
        }
        return true;
    }

    /// <inheritdoc />
    public virtual Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _isFirstCall = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine(GetType().Name);
    }

    /// <summary>
    /// The method is called before first ReadNext to initialize input.
    /// </summary>
    protected virtual ValueTask LoadAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
