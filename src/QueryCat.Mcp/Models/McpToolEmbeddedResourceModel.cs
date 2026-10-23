using System.Text.Json;

namespace QueryCat.Mcp.Models;

/// <summary>
/// The contents of a resource, embedded into a tool call result.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#embeddedresource.
/// </remarks>
public sealed class McpToolEmbeddedResourceModel : McpToolContentModel
{
    /// <summary>
    /// The resource contents.
    /// </summary>
    public JsonElement Resource { get; set; }
}
