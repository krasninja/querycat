using System.Text.Json.Serialization;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;

namespace QueryCat.WebServer;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WebServer.WebServerReply))]
[JsonSerializable(typeof(WebServer.WebServerQueryData))]
[JsonSerializable(typeof(WebServer.WebServerQueryDataParameter))]
[JsonSerializable(typeof(PluginInfo))]
[JsonSerializable(typeof(List<PluginInfo>))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonConverter(typeof(VariantValueJsonConverter))]
internal partial class SourceGenerationContext : JsonSerializerContext;
