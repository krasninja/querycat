namespace QueryCat.Backend.Core.Plugins.Auth;

/// <summary>
/// Authentication provider type.
/// </summary>
public enum AuthType
{
    /// <summary>
    /// OAuth 2.0 authorization (authorization code with PKCE or device flow).
    /// </summary>
    OAuth2 = 0,

    /// <summary>
    /// Static API key or personal access token provided by user.
    /// </summary>
    ApiKey = 1,
}
