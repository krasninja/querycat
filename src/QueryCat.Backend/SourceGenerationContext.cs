using System.Text.Json.Serialization;
using QueryCat.Backend.Execution;

namespace QueryCat.Backend;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(ConfigDictionary))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(Dictionary<string, object>))]
internal partial class SourceGenerationContext : JsonSerializerContext;
