using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Plugins.Auth;

namespace QueryCat.Backend.Auth;

/// <summary>
/// Default token provider. Reads tokens from the store and refreshes OAuth2 tokens on demand
/// when they are about to expire.
/// </summary>
public sealed class DefaultAuthTokenManager : IAuthTokenManager
{
    /// <summary>
    /// Refresh the token if it expires within this time.
    /// </summary>
    public static TimeSpan ExpirationSkew { get; } = TimeSpan.FromSeconds(60);

    private readonly IAuthTokenStore _store;
    private readonly OAuth2Client _oauth2Client;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(DefaultAuthTokenManager));

    /// <inheritdoc />
    public IAuthProvidersRegistry Providers { get; }

    /// <summary>
    /// Tokens store.
    /// </summary>
    public IAuthTokenStore Store => _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="store">Tokens store.</param>
    /// <param name="oauth2Client">OAuth2 client.</param>
    /// <param name="providers">Providers registry.</param>
    public DefaultAuthTokenManager(
        IAuthTokenStore store,
        OAuth2Client? oauth2Client = null,
        IAuthProvidersRegistry? providers = null)
    {
        _store = store;
        _oauth2Client = oauth2Client ?? new OAuth2Client();
        Providers = providers ?? new AuthProvidersRegistry();
    }

    /// <inheritdoc />
    public async ValueTask<AuthToken?> GetTokenOrDefaultAsync(
        string providerId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var stored = await _store.GetAsync(providerId, cancellationToken);
        if (stored == null || string.IsNullOrEmpty(stored.AccessToken))
        {
            return null;
        }
        var isExpired = stored.IsExpired(ExpirationSkew);
        if (!isExpired && !forceRefresh)
        {
            return stored.ToAuthToken();
        }

        var provider = Providers.Find(providerId);
        if (stored.Type == AuthType.ApiKey || string.IsNullOrEmpty(stored.RefreshToken)
            || provider == null || string.IsNullOrEmpty(provider.TokenUrl))
        {
            // Cannot refresh. Return the token while it is still valid.
            return stored.IsExpired() ? null : stored.ToAuthToken();
        }

        var refreshLock = _refreshLocks.GetOrAdd(providerId, _ => new SemaphoreSlim(1, 1));
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Another caller might refresh the token while we were waiting.
            var current = await _store.GetAsync(providerId, cancellationToken);
            if (current == null || string.IsNullOrEmpty(current.AccessToken))
            {
                return null;
            }
            if (current.UpdatedAt != stored.UpdatedAt && !current.IsExpired(ExpirationSkew))
            {
                return current.ToAuthToken();
            }

            return await RefreshAsync(providerId, provider, current, cancellationToken);
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private async Task<AuthToken?> RefreshAsync(
        string providerId,
        AuthProviderInfo provider,
        StoredAuthToken current,
        CancellationToken cancellationToken)
    {
        var clientId = current.ClientId ?? provider.ClientId;
        var clientSecret = current.ClientSecret ?? provider.ClientSecret;
        _logger.LogDebug("Refresh token for provider '{ProviderId}'.", providerId);
        try
        {
            var response = await _oauth2Client.RefreshAsync(provider, clientId, clientSecret, current.RefreshToken!,
                cancellationToken);
            var updated = OAuth2Client.CreateStoredToken(response, current);
            await _store.SetAsync(providerId, updated, cancellationToken);
            return updated.ToAuthToken();
        }
        catch (Exception e) when (e is QueryCatException or HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Cannot refresh token for provider '{ProviderId}': {Error}", providerId, e.Message);
            return current.IsExpired() ? null : current.ToAuthToken();
        }
    }
}
