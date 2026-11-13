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

    /// <summary>
    /// Validation results by provider. The key is provider id, the value is the validated access token
    /// and the result. The token is validated again if the access token changes.
    /// </summary>
    private readonly ConcurrentDictionary<string, (string AccessToken, bool IsValid)> _validatedTokens
        = new(StringComparer.OrdinalIgnoreCase);

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
        var provider = Providers.Find(providerId);
        var isExpired = stored.IsExpired(ExpirationSkew);
        if (!isExpired && !forceRefresh)
        {
            if (provider != null && !string.IsNullOrEmpty(provider.TokenValidateUrl))
            {
                if (_validatedTokens.TryGetValue(providerId, out var validated)
                    && validated.AccessToken == stored.AccessToken)
                {
                    return validated.IsValid ? stored.ToAuthToken() : null;
                }
                return await ValidateAsync(providerId, provider, cancellationToken);
            }
            return stored.ToAuthToken();
        }

        if (provider == null || !CanRefresh(stored, provider))
        {
            // Cannot refresh. Return the token while it is still valid.
            return stored.IsExpired() ? null : stored.ToAuthToken();
        }

        var refreshLock = GetRefreshLock(providerId);
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

            var updated = await RefreshAsync(providerId, provider, current, cancellationToken);
            if (updated != null)
            {
                return updated.ToAuthToken();
            }
            return current.IsExpired() ? null : current.ToAuthToken();
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private async Task<AuthToken?> ValidateAsync(
        string providerId,
        AuthProviderInfo provider,
        CancellationToken cancellationToken)
    {
        var refreshLock = GetRefreshLock(providerId);
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Re-read the token, another caller might validate or refresh it while we were waiting.
            var current = await _store.GetAsync(providerId, cancellationToken);
            if (current == null || string.IsNullOrEmpty(current.AccessToken))
            {
                return null;
            }
            if (_validatedTokens.TryGetValue(providerId, out var validated)
                && validated.AccessToken == current.AccessToken)
            {
                return validated.IsValid ? current.ToAuthToken() : null;
            }

            _logger.LogDebug("Validate token for provider '{ProviderId}'.", providerId);
            var token = current.ToAuthToken();
            var isValid = await _oauth2Client.ValidateTokenAsync(provider.TokenValidateUrl!, token, cancellationToken);
            if (isValid == true)
            {
                _validatedTokens[providerId] = (current.AccessToken, true);
                return token;
            }
            if (isValid == null)
            {
                // Cannot determine, do not cache the result to try again next time.
                _logger.LogDebug("Cannot validate token for provider '{ProviderId}'.", providerId);
                return token;
            }

            _logger.LogWarning("Token for provider '{ProviderId}' is rejected by the server.", providerId);
            if (CanRefresh(current, provider))
            {
                var updated = await RefreshAsync(providerId, provider, current, cancellationToken);
                if (updated != null)
                {
                    return updated.ToAuthToken();
                }
            }
            _validatedTokens[providerId] = (current.AccessToken, false);
            return null;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private async Task<StoredAuthToken?> RefreshAsync(
        string providerId,
        AuthProviderInfo provider,
        StoredAuthToken current,
        CancellationToken cancellationToken)
    {
        var clientId = !string.IsNullOrEmpty(current.ClientId) ? current.ClientId : provider.ClientId;
        var clientSecret = !string.IsNullOrEmpty(current.ClientSecret) ? current.ClientSecret : provider.ClientSecret;
        _logger.LogDebug("Refresh token for provider '{ProviderId}'.", providerId);
        try
        {
            var response = await _oauth2Client.RefreshAsync(provider, clientId, clientSecret, current.RefreshToken,
                cancellationToken);
            var updated = OAuth2Client.CreateStoredToken(response, current);
            await _store.SetAsync(providerId, updated, cancellationToken);
            // The token has just been issued by the server, no need to validate it.
            _validatedTokens[providerId] = (updated.AccessToken, true);
            return updated;
        }
        catch (Exception e) when (e is QueryCatException or HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Cannot refresh token for provider '{ProviderId}': {Error}", providerId, e.Message);
            return null;
        }
    }

    private static bool CanRefresh(StoredAuthToken token, AuthProviderInfo provider)
        => token.Type != AuthType.ApiKey
            && !string.IsNullOrEmpty(token.RefreshToken)
            && (!string.IsNullOrEmpty(provider.TokenUrl) || !string.IsNullOrEmpty(provider.RefreshTokenUrl));

    private SemaphoreSlim GetRefreshLock(string providerId)
        => _refreshLocks.GetOrAdd(providerId, _ => new SemaphoreSlim(1, 1));
}
