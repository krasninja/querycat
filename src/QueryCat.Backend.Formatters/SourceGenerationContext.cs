using System.Text.Json;
using System.Text.Json.Serialization;

namespace QueryCat.Backend.Formatters;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(JsonElement))]
internal partial class SourceGenerationContext : JsonSerializerContext;
