using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Inputs;

internal class RetryRowsSource : IRowsSource
{
    private readonly IRowsSource _source;
    private readonly int _maxAttempts;
    private readonly TimeSpan _retryInterval;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(RetryRowsSource));

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _source.QueryContext;
        set => _source.QueryContext = value;
    }

    public RetryRowsSource(IRowsSource source, int maxAttempts = 3, TimeSpan? retryInterval = null)
    {
        _source = source;
        _maxAttempts = maxAttempts;
        _retryInterval = retryInterval ?? TimeSpan.FromSeconds(5);
    }

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        await RetryCoreAsync(
            async ct =>
            {
                await _source.OpenAsync(ct).ConfigureAwait(false);
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await RetryCoreAsync(
            async ct =>
            {
                await _source.CloseAsync(ct).ConfigureAwait(false);
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await RetryCoreAsync(
            async ct =>
            {
                await _source.ResetAsync(ct).ConfigureAwait(false);
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }

    protected ValueTask<TResult> RetryWrapperAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> func,
        CancellationToken cancellationToken)
        => RetryCoreAsync(func, cancellationToken);

    protected ValueTask<TResult> RetryWrapperAsync<T1, TResult>(
        Func<T1, CancellationToken, ValueTask<TResult>> func,
        T1 arg1,
        CancellationToken cancellationToken)
        => RetryCoreAsync(ct => func.Invoke(arg1, ct), cancellationToken);

    private async ValueTask<TResult> RetryCoreAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> func,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < _maxAttempts; i++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await func.Invoke(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "Operation failed. Attempt {AttemptNumber} of {MaxAttempts}.", i + 1, _maxAttempts);
                if (i == _maxAttempts - 1)
                {
                    throw;
                }
                await Task.Delay(_retryInterval, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new QueryCatException(Resources.Errors.InvalidOperation);
    }
}
