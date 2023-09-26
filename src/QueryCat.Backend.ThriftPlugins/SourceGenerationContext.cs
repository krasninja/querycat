using System.Text.Json.Serialization;

namespace QueryCat.Backend.ThriftPlugins;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(ThriftPluginsLoader.FunctionsCache))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
