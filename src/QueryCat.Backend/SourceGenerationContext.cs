using System.Text.Json.Serialization;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Inputs;

namespace QueryCat.Backend;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(ConfigDictionary))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(AIAssistant.PromptResponseModel))]
internal partial class SourceGenerationContext : JsonSerializerContext;
