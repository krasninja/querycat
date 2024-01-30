using System.Text.Json.Serialization;
using QueryCat.Backend.Core.Types;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WebServer.WebServerReply))]
[JsonSerializable(typeof(WebServer.WebServerQueryData))]
[JsonSerializable(typeof(WebServer.WebServerQueryDataParameter))]
[JsonSerializable(typeof(Backend.Core.Plugins.PluginInfo))]
[JsonSerializable(typeof(List<Backend.Core.Plugins.PluginInfo>))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonConverter(typeof(VariantValueJsonConverter))]
internal partial class SourceGenerationContext : JsonSerializerContext;
