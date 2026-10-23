using System.Text.Json;
using System.Text.Json.Serialization;
using QueryCat.Mcp.Models;
using QueryCat.Mcp.Tools;

namespace QueryCat.Mcp;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(JsonRpcRequest))]
[JsonSerializable(typeof(JsonRpcResponse))]
[JsonSerializable(typeof(JsonRpcErrorResponse))]
[JsonSerializable(typeof(McpInitializeResultModel))]
[JsonSerializable(typeof(McpImplementationModel))]
[JsonSerializable(typeof(McpServerCapabilitiesModel))]
[JsonSerializable(typeof(McpServerToolsCapabilityModel))]
[JsonSerializable(typeof(McpClientCapabilitiesModel))]
[JsonSerializable(typeof(McpEmptyResultModel))]
[JsonSerializable(typeof(McpListToolsResultModel))]
[JsonSerializable(typeof(McpCallToolResultModel))]
[JsonSerializable(typeof(McpToolModel))]
[JsonSerializable(typeof(McpToolAnnotationsModel))]
[JsonSerializable(typeof(McpToolInputSchemaModel))]
[JsonSerializable(typeof(McpToolContentModel))]
[JsonSerializable(typeof(McpToolTextContentModel))]
[JsonSerializable(typeof(McpToolImageContentModel))]
[JsonSerializable(typeof(McpToolAudioContentModel))]
[JsonSerializable(typeof(McpToolResourceLinkModel))]
[JsonSerializable(typeof(McpToolEmbeddedResourceModel))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(SearchFunctionsTool.SearchFunctionsToolFunctionResponse))]
[JsonSerializable(typeof(SearchFunctionsTool.SearchFunctionsToolResponse))]
internal partial class SourceGenerationContext : JsonSerializerContext;
