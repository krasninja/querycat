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
    public abstract Task OpenAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task CloseAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _isFirstCall = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public RowsOutputOptions Options { get; protected set; } = new();

    /// <inheritdoc />
    public async ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        if (_isFirstCall)
        {
            await InitializeAsync(cancellationToken);
            _isFirstCall = false;
        }
        var result = await OnWriteAsync(values, cancellationToken);
        return result;
    }

    /// <summary>
    /// Write a row.
    /// </summary>
    /// <param name="values">Values to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected abstract ValueTask<ErrorCode> OnWriteAsync(VariantValue[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// The method is called before first Write to initialize input.
    /// </summary>
    protected virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
