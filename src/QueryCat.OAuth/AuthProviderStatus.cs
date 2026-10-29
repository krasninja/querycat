using QueryCat.Backend.Core.Plugins.Auth;

namespace QueryCat.OAuth;

/// <summary>
/// Authentication provider and its login status.
/// </summary>
/// <param name="Id">Authentication provider identifier.</param>
/// <param name="Plugin">Plugin name. Empty if the plugin has been removed.</param>
/// <param name="ProviderType">Supported authentication types.</param>
/// <param name="Status">Login status.</param>
/// <param name="TokenType">Stored credentials type.</param>
/// <param name="ExpiresAt">Access token expiration time.</param>
/// <param name="Scopes">Granted scopes.</param>
public sealed record AuthProviderStatus(
    string Id,
    string Plugin,
    string ProviderType,
    string Status,
    AuthType? TokenType,
    DateTime? ExpiresAt,
    string Scopes)
{
    public const string StatusLoggedIn = "logged_in";
    public const string StatusExpired = "expired";
    public const string StatusNotLoggedIn = "not_logged_in";
}
