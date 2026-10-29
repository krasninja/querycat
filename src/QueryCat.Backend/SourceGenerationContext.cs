using System.Text.Json.Serialization;
using QueryCat.Backend.Auth;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Inputs;

namespace QueryCat.Backend;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(ConfigDictionary))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(AIAssistant.PromptResponseModel))]
[JsonSerializable(typeof(PluginsManager.NginxPluginsStorage.NginxObjectDto))]
[JsonSerializable(typeof(IList<PluginsManager.NginxPluginsStorage.NginxObjectDto>))]
[JsonSerializable(typeof(Dictionary<string, StoredAuthToken>))]
[JsonSerializable(typeof(OAuth2Client.OAuth2TokenResponse))]
[JsonSerializable(typeof(OAuth2Client.OAuth2DeviceAuthorizationResponse))]
internal partial class SourceGenerationContext : JsonSerializerContext;
