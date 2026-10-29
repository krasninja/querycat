using System.Text.Json.Serialization;
using QueryCat.Backend.Core.Plugins.Auth;

namespace QueryCat.Backend.Auth;

/// <summary>
/// Persisted authentication token with refresh information.
/// </summary>
public sealed class StoredAuthToken
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<AuthType>))]
    public AuthType Type { get; set; } = AuthType.OAuth2;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Expiration time in Unix seconds.
    /// </summary>
    [JsonPropertyName("expires_at")]
    public long? ExpiresAt { get; set; }

    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; set; } = [];

    /// <summary>
    /// Client identifier used for login. It is needed for refresh if user overrides the plugin one.
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Last update time in Unix milliseconds.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public long UpdatedAt { get; set; }

    /// <summary>
    /// Returns true if the token expires within the specified time.
    /// </summary>
    /// <param name="skew">Time before actual expiration.</param>
    /// <returns>True if expired.</returns>
    public bool IsExpired(TimeSpan skew = default)
        => ExpiresAt.HasValue && DateTimeOffset.FromUnixTimeSeconds(ExpiresAt.Value) - skew <= DateTimeOffset.UtcNow;

    /// <summary>
    /// Convert to public token model.
    /// </summary>
    /// <returns>Instance of <see cref="AuthToken" />.</returns>
    public AuthToken ToAuthToken() => new()
    {
        AccessToken = AccessToken,
        TokenType = string.IsNullOrEmpty(TokenType) ? "Bearer" : TokenType,
        ExpiresAt = ExpiresAt.HasValue ? DateTimeOffset.FromUnixTimeSeconds(ExpiresAt.Value) : null,
        Scopes = Scopes,
    };
}
