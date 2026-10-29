namespace QueryCat.OAuth;

/// <summary>
/// Login options.
/// </summary>
public sealed class AuthLoginOptions
{
    /// <summary>
    /// Use OAuth2 device authorization flow.
    /// </summary>
    public bool UseDeviceFlow { get; init; }

    /// <summary>
    /// Use API key instead of OAuth2 login.
    /// </summary>
    public bool UseApiKey { get; init; }

    /// <summary>
    /// Open authorization URL in the default browser.
    /// </summary>
    public bool OpenBrowser { get; init; } = true;

    /// <summary>
    /// Override OAuth2 client identifier provided by plugin.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Override OAuth2 client secret provided by plugin.
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// Max time to wait for the authorization redirect.
    /// </summary>
    public TimeSpan CallbackTimeout { get; init; } = TimeSpan.FromMinutes(5);
}
