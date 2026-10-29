using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Plugins.Auth;

namespace QueryCat.Backend.Auth;

/// <summary>
/// Minimal OAuth2 client: authorization code with PKCE, device authorization and refresh token grants.
/// </summary>
public sealed class OAuth2Client
{
    private const string DeviceCodeGrantType = "urn:ietf:params:oauth:grant-type:device_code";

    private static readonly TimeSpan _slowDownIncrement = TimeSpan.FromSeconds(5);

    private static readonly HttpClient _sharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="httpClient">Custom HTTP client. Shared instance is used if null.</param>
    public OAuth2Client(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? _sharedHttpClient;
    }

    /// <summary>
    /// Create PKCE verifier and S256 challenge.
    /// </summary>
    /// <returns>Instance of <see cref="OAuth2Pkce" />.</returns>
    public static OAuth2Pkce CreatePkce()
    {
        var verifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return new OAuth2Pkce(verifier, challenge);
    }

    /// <summary>
    /// Create random state value to prevent CSRF.
    /// </summary>
    /// <returns>State.</returns>
    public static string CreateState() => Base64UrlEncode(RandomNumberGenerator.GetBytes(16));

    /// <summary>
    /// Build authorization URL for authorization code flow.
    /// </summary>
    /// <param name="provider">Provider.</param>
    /// <param name="clientId">Client identifier.</param>
    /// <param name="redirectUri">Redirect URI.</param>
    /// <param name="state">State.</param>
    /// <param name="pkce">PKCE.</param>
    /// <returns>URL to open in browser.</returns>
    public static string BuildAuthorizationUrl(
        AuthProviderInfo provider,
        string clientId,
        string redirectUri,
        string state,
        OAuth2Pkce pkce)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("response_type", "code"),
            new("client_id", clientId),
            new("redirect_uri", redirectUri),
            new("state", state),
            new("code_challenge", pkce.Challenge),
            new("code_challenge_method", "S256"),
        };
        if (provider.Scopes.Count > 0)
        {
            parameters.Add(new("scope", string.Join(' ', provider.Scopes)));
        }
        parameters.AddRange(provider.ExtraParams);

        var query = string.Join('&', parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        var url = provider.AuthorizationUrl;
        return url + (url.Contains('?') ? '&' : '?') + query;
    }

    /// <summary>
    /// Exchange authorization code to token.
    /// </summary>
    /// <param name="provider">Provider.</param>
    /// <param name="clientId">Client identifier.</param>
    /// <param name="clientSecret">Client secret, optional.</param>
    /// <param name="code">Authorization code.</param>
    /// <param name="redirectUri">Redirect URI used in authorization request.</param>
    /// <param name="pkce">PKCE used in authorization request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token response.</returns>
    public async Task<OAuth2TokenResponse> ExchangeCodeAsync(
        AuthProviderInfo provider,
        string clientId,
        string? clientSecret,
        string code,
        string redirectUri,
        OAuth2Pkce pkce,
        CancellationToken cancellationToken = default)
    {
        var response = await PostTokenAsync(provider, clientId, clientSecret,
        [
            new("grant_type", "authorization_code"),
            new("code", code),
            new("redirect_uri", redirectUri),
            new("code_verifier", pkce.Verifier),
        ], cancellationToken);
        return EnsureSuccess(response);
    }

    /// <summary>
    /// Refresh the access token.
    /// </summary>
    /// <param name="provider">Provider.</param>
    /// <param name="clientId">Client identifier.</param>
    /// <param name="clientSecret">Client secret, optional.</param>
    /// <param name="refreshToken">Refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token response.</returns>
    public async Task<OAuth2TokenResponse> RefreshAsync(
        AuthProviderInfo provider,
        string clientId,
        string? clientSecret,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var response = await PostTokenAsync(provider, clientId, clientSecret,
        [
            new("grant_type", "refresh_token"),
            new("refresh_token", refreshToken),
        ], cancellationToken);
        return EnsureSuccess(response);
    }

    /// <summary>
    /// Start device authorization flow.
    /// </summary>
    /// <param name="provider">Provider.</param>
    /// <param name="clientId">Client identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Device authorization response with user code.</returns>
    public async Task<OAuth2DeviceAuthorizationResponse> StartDeviceAuthorizationAsync(
        AuthProviderInfo provider,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("client_id", clientId),
        };
        if (provider.Scopes.Count > 0)
        {
            parameters.Add(new("scope", string.Join(' ', provider.Scopes)));
        }
        parameters.AddRange(provider.ExtraParams);

        var (response, statusCode) = await PostAsync(provider.DeviceAuthorizationUrl, parameters,
            SourceGenerationContext.Default.OAuth2DeviceAuthorizationResponse, cancellationToken);
        if (!string.IsNullOrEmpty(response?.Error))
        {
            throw new QueryCatException(string.Format(Resources.Errors.OAuth2Error, response.Error, response.ErrorDescription));
        }
        if (response == null || string.IsNullOrEmpty(response.DeviceCode) || string.IsNullOrEmpty(response.Uri))
        {
            throw new QueryCatException(string.Format(Resources.Errors.OAuth2InvalidResponse,
                provider.DeviceAuthorizationUrl, statusCode));
        }
        return response;
    }

    /// <summary>
    /// Poll token endpoint until user completes device authorization.
    /// </summary>
    /// <param name="provider">Provider.</param>
    /// <param name="clientId">Client identifier.</param>
    /// <param name="clientSecret">Client secret, optional.</param>
    /// <param name="device">Device authorization response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token response.</returns>
    public async Task<OAuth2TokenResponse> PollDeviceTokenAsync(
        AuthProviderInfo provider,
        string clientId,
        string? clientSecret,
        OAuth2DeviceAuthorizationResponse device,
        CancellationToken cancellationToken = default)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, device.Interval));
        var deadline = DateTimeOffset.UtcNow.AddSeconds(device.ExpiresIn);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(interval, cancellationToken);
            var response = await PostTokenAsync(provider, clientId, clientSecret,
            [
                new("grant_type", DeviceCodeGrantType),
                new("device_code", device.DeviceCode),
            ], cancellationToken);
            switch (response.Error)
            {
                case "authorization_pending":
                    continue;
                case "slow_down":
                    interval += _slowDownIncrement;
                    continue;
                default:
                    return EnsureSuccess(response);
            }
        }
        throw new QueryCatException(Resources.Errors.OAuth2DeviceCodeExpired);
    }

    private async Task<OAuth2TokenResponse> PostTokenAsync(
        AuthProviderInfo provider,
        string clientId,
        string? clientSecret,
        List<KeyValuePair<string, string>> parameters,
        CancellationToken cancellationToken)
    {
        parameters.Add(new("client_id", clientId));
        if (!string.IsNullOrEmpty(clientSecret))
        {
            parameters.Add(new("client_secret", clientSecret));
        }
        var (response, statusCode) = await PostAsync(provider.TokenUrl, parameters,
            SourceGenerationContext.Default.OAuth2TokenResponse, cancellationToken);
        if (response == null)
        {
            throw new QueryCatException(string.Format(Resources.Errors.OAuth2InvalidResponse, provider.TokenUrl, statusCode));
        }
        return response;
    }

    private async Task<(T? Response, int StatusCode)> PostAsync<T>(
        string url,
        IEnumerable<KeyValuePair<string, string>> parameters,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new FormUrlEncodedContent(parameters);
        // Some providers (GitHub) return form-encoded response unless JSON is requested explicitly.
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var statusCode = (int)response.StatusCode;
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            return (JsonSerializer.Deserialize(content, typeInfo), statusCode);
        }
        catch (JsonException)
        {
            return (null, statusCode);
        }
    }

    private static OAuth2TokenResponse EnsureSuccess(OAuth2TokenResponse response)
    {
        if (!string.IsNullOrEmpty(response.Error))
        {
            throw new QueryCatException(string.Format(Resources.Errors.OAuth2Error, response.Error, response.ErrorDescription));
        }
        if (string.IsNullOrEmpty(response.AccessToken))
        {
            throw new QueryCatException(string.Format(Resources.Errors.OAuth2Error, "invalid_response", string.Empty));
        }
        return response;
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// Create stored token from token response.
    /// </summary>
    /// <param name="response">Token response.</param>
    /// <param name="previous">Previous token to keep refresh token if the new one is not issued.</param>
    /// <returns>Instance of <see cref="StoredAuthToken" />.</returns>
    public static StoredAuthToken CreateStoredToken(OAuth2TokenResponse response, StoredAuthToken? previous = null)
    {
        var scopes = !string.IsNullOrEmpty(response.Scope)
            ? response.Scope.Split([' ', ','],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : previous?.Scopes;
        return new StoredAuthToken
        {
            Type = AuthType.OAuth2,
            AccessToken = response.AccessToken,
            TokenType = response.TokenType,
            RefreshToken =
                (!string.IsNullOrEmpty(response.RefreshToken) ? response.RefreshToken : previous?.RefreshToken) ??
                string.Empty,
            ExpiresAt = response.ExpiresIn > 0
                ? DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn.Value).ToUnixTimeSeconds()
                : null,
            Scopes = scopes ?? [],
            ClientId = previous?.ClientId ?? string.Empty,
            ClientSecret = previous?.ClientSecret ?? string.Empty,
        };
    }

    /// <summary>
    /// OAuth2 token endpoint response (RFC 6749, section 5).
    /// </summary>
    public sealed class OAuth2TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }

    /// <summary>
    /// OAuth2 device authorization response (RFC 8628, section 3.2).
    /// </summary>
    public sealed class OAuth2DeviceAuthorizationResponse
    {
        [JsonPropertyName("device_code")]
        public string DeviceCode { get; set; } = string.Empty;

        [JsonPropertyName("user_code")]
        public string UserCode { get; set; } = string.Empty;

        [JsonPropertyName("verification_uri")]
        public string? VerificationUri { get; set; }

        /// <summary>
        /// Non-standard name used by some providers (f.e. Google).
        /// </summary>
        [JsonPropertyName("verification_url")]
        public string? VerificationUrl { get; set; }

        [JsonPropertyName("verification_uri_complete")]
        public string? VerificationUriComplete { get; set; }

        [JsonPropertyName("expires_in")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long ExpiresIn { get; set; } = 900;

        [JsonPropertyName("interval")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Interval { get; set; } = 5;

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }

        /// <summary>
        /// The URI the user should visit.
        /// </summary>
        [JsonIgnore]
        public string Uri => VerificationUri ?? VerificationUrl ?? string.Empty;
    }

    /// <summary>
    /// PKCE code verifier and challenge (RFC 7636).
    /// </summary>
    /// <param name="Verifier">Code verifier.</param>
    /// <param name="Challenge">S256 code challenge.</param>
    public sealed record OAuth2Pkce(string Verifier, string Challenge);
}
