namespace QueryCat.Backend.Core.Plugins.Auth;

/// <summary>
/// Extensions for <see cref="IAuthTokenManager" />.
/// </summary>
public static class AuthTokenProviderExtensions
{
    /// <summary>
    /// Get active token for the provider or throw <see cref="AuthRequiredException" /> if user is not logged in.
    /// </summary>
    /// <param name="manager">Token provider.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="forceRefresh">Refresh token even if it is not expired.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token.</returns>
    public static async ValueTask<AuthToken> GetTokenAsync(
        this IAuthTokenManager manager,
        string providerId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var token = await manager.GetTokenOrDefaultAsync(providerId, forceRefresh, cancellationToken);
        if (token == null)
        {
            throw new AuthRequiredException(providerId);
        }
        return token;
    }
}
