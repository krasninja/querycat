namespace QueryCat.Backend.Core.Plugins.Auth;

/// <summary>
/// Provides active authentication tokens. The implementation is responsible for token refresh.
/// </summary>
public interface IAuthTokenManager
{
    /// <summary>
    /// Authentication providers.
    /// </summary>
    IAuthProvidersRegistry Providers { get; }

    /// <summary>
    /// Get active token for the provider.
    /// </summary>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="forceRefresh">Refresh token even if it is not expired. Use it after HTTP 401 response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token or null if user is not logged in.</returns>
    ValueTask<AuthToken?> GetTokenOrDefaultAsync(
        string providerId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);
}
