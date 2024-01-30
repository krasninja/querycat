using System.Text.Json;
using System.Text.Json.Serialization;

namespace QueryCat.Backend.Addons;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Dictionary<string, object>))]
internal partial class SourceGenerationContext : JsonSerializerContext;
