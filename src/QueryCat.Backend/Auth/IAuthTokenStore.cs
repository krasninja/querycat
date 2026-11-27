namespace QueryCat.Backend.Auth;

/// <summary>
/// Persistent storage of authentication tokens.
/// </summary>
public interface IAuthTokenStore
{
    /// <summary>
    /// Get token by provider identifier.
    /// </summary>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token or null if not found.</returns>
    Task<StoredAuthToken?> GetAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all stored tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Map of provider identifier and token.</returns>
    Task<IReadOnlyDictionary<string, StoredAuthToken>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Save token.
    /// </summary>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="token">Token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task SetAsync(string providerId, StoredAuthToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove token.
    /// </summary>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if token was removed, false if not found.</returns>
    Task<bool> RemoveAsync(string providerId, CancellationToken cancellationToken = default);
}
