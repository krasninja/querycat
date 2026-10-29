namespace QueryCat.Backend.Core.Plugins.Auth;

/// <summary>
/// Authentication provider declared by plugin. The QueryCat host uses it to perform
/// login and refresh the tokens.
/// </summary>
public sealed class AuthProviderInfo
{
    /// <summary>
    /// Provider identifier (f.e. "github"). Must be unique across all plugins.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Provider type.
    /// </summary>
    public AuthType Type { get; init; } = AuthType.OAuth2;

    /// <summary>
    /// User-friendly name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// OAuth2 authorization endpoint.
    /// </summary>
    public string AuthorizationUrl { get; init; } = string.Empty;

    /// <summary>
    /// OAuth2 token endpoint.
    /// </summary>
    public string TokenUrl { get; init; } = string.Empty;

    /// <summary>
    /// OAuth2 device authorization endpoint (RFC 8628).
    /// </summary>
    public string DeviceAuthorizationUrl { get; init; } = string.Empty;

    /// <summary>
    /// OAuth2 client identifier.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// OAuth2 client secret. Plugins are public clients, so prefer PKCE or device flow without secret.
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Requested scopes.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>
    /// Additional authorization request parameters (audience, prompt, access_type, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, string> ExtraParams { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Fixed loopback redirect port. Random port is used if 0.
    /// </summary>
    public int RedirectPort { get; init; }

    /// <summary>
    /// Use device authorization flow by default.
    /// </summary>
    public bool PreferDeviceFlow { get; init; }

    /// <summary>
    /// Allow user to provide API key or personal access token instead of OAuth2 login.
    /// </summary>
    public bool AllowApiKey { get; init; }

    /// <summary>
    /// Name of the plugin that declared the provider. It is set by the host.
    /// </summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// Is OAuth2 device flow available.
    /// </summary>
    public bool SupportsDeviceFlow => Type == AuthType.OAuth2
        && !string.IsNullOrEmpty(DeviceAuthorizationUrl) && !string.IsNullOrEmpty(TokenUrl);

    /// <summary>
    /// Is OAuth2 authorization code flow available.
    /// </summary>
    public bool SupportsAuthorizationCodeFlow => Type == AuthType.OAuth2
        && !string.IsNullOrEmpty(AuthorizationUrl) && !string.IsNullOrEmpty(TokenUrl);

    /// <inheritdoc />
    public override string ToString() => Id;
}
