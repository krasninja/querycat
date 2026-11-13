using System.Text.Json.Serialization;
using QueryCat.Backend.Core.Plugins.Auth;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed record PluginContextAuth(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] AuthType Type,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("auth_url")] string AuthorizationUrl,
    [property: JsonPropertyName("token_url")] string TokenUrl,
    [property: JsonPropertyName("device_auth_url")] string DeviceAuthorizationUrl,
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("client_secret")] string ClientSecret,
    [property: JsonPropertyName("scopes")] string[]? Scopes,
    [property: JsonPropertyName("extra")] Dictionary<string, string>? ExtraParams,
    [property: JsonPropertyName("redirect_port")] int RedirectPort,
    [property: JsonPropertyName("redirect_loopback")] string? RedirectLoopback,
    [property: JsonPropertyName("redirect_path")] string? RedirectPath,
    [property: JsonPropertyName("refresh_token_url")] string? RefreshTokenUrl,
    [property: JsonPropertyName("refresh_token_param_name")] string? RefreshTokenParamName,
    [property: JsonPropertyName("device_flow")] bool PreferDeviceFlow,
    [property: JsonPropertyName("allow_api_key")] bool AllowApiKey,
    [property: JsonPropertyName("disable_pkce")] bool DisablePkce,
    [property: JsonPropertyName("token_validate_url")] string? TokenValidateUrl,
    [property: JsonPropertyName("plugin_name")] string PluginName
);
