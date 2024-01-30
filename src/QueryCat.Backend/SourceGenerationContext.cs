using System.Text.Json.Serialization;
using QueryCat.Backend.Execution;

namespace QueryCat.Backend;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(ConfigDictionary))]
[JsonSerializable(typeof(DateTimeOffset))]
internal partial class SourceGenerationContext : JsonSerializerContext;
