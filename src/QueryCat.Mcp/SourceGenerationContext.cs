using System.Text.Json.Serialization;
using QueryCat.Mcp.Models;

namespace QueryCat.Mcp;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(JsonRpcRequest))]
[JsonSerializable(typeof(JsonRpcResponse))]
[JsonSerializable(typeof(JsonRpcErrorResponse))]
[JsonSerializable(typeof(McpInitializeResultModel))]
[JsonSerializable(typeof(McpImplementationModel))]
[JsonSerializable(typeof(McpServerCapabilitiesModel))]
[JsonSerializable(typeof(McpClientCapabilitiesModel))]
[JsonSerializable(typeof(McpEmptyResultModel))]
internal partial class SourceGenerationContext : JsonSerializerContext;
