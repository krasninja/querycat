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
    [property: JsonPropertyName("device_flow")] bool PreferDeviceFlow,
    [property: JsonPropertyName("allow_api_key")] bool AllowApiKey,
    [property: JsonPropertyName("plugin_name")] string PluginName
);
