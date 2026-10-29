using System.Text.Json.Serialization;

namespace QueryCat.Backend.ThriftPlugins;

[JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ThriftPluginsLoader.PluginsFunctionsCache))]
[JsonSerializable(typeof(PluginContextAuth))]
[JsonSerializable(typeof(PluginContextFunction))]
internal partial class SourceGenerationContext : JsonSerializerContext;
