using System.Text.Json.Serialization;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WebServer.WebServerReply))]
[JsonSerializable(typeof(WebServer.QueryWrapper))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
