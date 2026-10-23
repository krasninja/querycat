using System.Text.Json.Serialization;

namespace QueryCat.Mcp.Models;

/// <summary>
/// Base class for content returned from a tool call. The actual type is determined by the
/// <c>type</c> discriminator property.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#content.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(McpToolTextContentModel), "text")]
[JsonDerivedType(typeof(McpToolImageContentModel), "image")]
[JsonDerivedType(typeof(McpToolAudioContentModel), "audio")]
[JsonDerivedType(typeof(McpToolResourceLinkModel), "resource_link")]
[JsonDerivedType(typeof(McpToolEmbeddedResourceModel), "resource")]
public abstract class McpToolContentModel;
