namespace QueryCat.Backend.Core.Plugins.Auth;

/// <summary>
/// Active authentication token.
/// </summary>
public sealed class AuthToken
{
    /// <summary>
    /// Access token or API key.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Token type, usually "Bearer".
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Expiration time. Null if the token does not expire.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Granted scopes.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>
    /// Returns true if the token expires within the specified time.
    /// </summary>
    /// <param name="skew">Time before actual expiration.</param>
    /// <returns>True if expired.</returns>
    public bool IsExpired(TimeSpan skew = default)
        => ExpiresAt.HasValue && ExpiresAt.Value - skew <= DateTimeOffset.UtcNow;
}
