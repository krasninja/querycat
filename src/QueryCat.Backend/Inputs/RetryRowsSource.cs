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
    public Task OpenAsync(CancellationToken cancellationToken = default) => _source.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) => _source.CloseAsync(cancellationToken);

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) => _source.ResetAsync(cancellationToken);

    protected ValueTask<TResult> RetryWrapperAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> func,
        CancellationToken cancellationToken)
        => RetryWrapperAsync(
            (_, _, _, ct) => func.Invoke(ct),
            1,
            1,
            1,
            cancellationToken);

    protected ValueTask<TResult> RetryWrapperAsync<T1, TResult>(
        Func<T1, CancellationToken, ValueTask<TResult>> func,
        T1 arg1,
        CancellationToken cancellationToken)
        => RetryWrapperAsync(
            (a1, _, _, ct) => func.Invoke(a1, ct),
            arg1,
            1,
            1,
            cancellationToken);

    protected ValueTask<TResult> RetryWrapperAsync<T1, T2, TResult>(
        Func<T1, T2, CancellationToken, ValueTask<TResult>> func,
        T1 arg1,
        T2 arg2,
        CancellationToken cancellationToken)
        => RetryWrapperAsync(
            (a1, a2, _, ct) => func.Invoke(a1, a2, ct),
            arg1,
            arg2,
            1,
            cancellationToken);

    protected async ValueTask<TResult> RetryWrapperAsync<T1, T2, T3, TResult>(
        Func<T1, T2, T3, CancellationToken, ValueTask<TResult>> func,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < _maxAttempts; i++)
        {
            try
            {
                return await func.Invoke(arg1, arg2, arg3, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "Failed to read rows. Attempt {AttemptNumber}.", i + 1);
                await Task.Delay(_retryInterval, cancellationToken);
                if (i == _maxAttempts - 1)
                {
                    throw;
                }
            }
        }

        throw new QueryCatException(Resources.Errors.InvalidOperation);
    }
}
