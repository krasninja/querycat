using QueryCat.Backend.Core.Plugins.Auth;

namespace QueryCat.OAuth;

/// <summary>
/// User interaction during login.
/// </summary>
public interface IAuthLoginInteraction
{
    /// <summary>
    /// Ask user for API key.
    /// </summary>
    /// <param name="provider">Authentication provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API key.</returns>
    ValueTask<string> ReadApiKeyAsync(AuthProviderInfo provider, CancellationToken cancellationToken);

    /// <summary>
    /// Show URL user should open to authorize (authorization code flow).
    /// </summary>
    /// <param name="url">Authorization URL.</param>
    void ShowAuthorizationUrl(string url);

    /// <summary>
    /// Show verification URI and user code (device flow).
    /// </summary>
    /// <param name="verificationUri">Verification URI.</param>
    /// <param name="userCode">User code.</param>
    void ShowDeviceCode(string verificationUri, string userCode);

    /// <summary>
    /// Notify that authorization is in progress.
    /// </summary>
    void ShowWaiting();
}
