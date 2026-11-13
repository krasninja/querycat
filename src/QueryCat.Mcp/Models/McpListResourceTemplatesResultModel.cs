using System.Text.Json;

namespace QueryCat.Mcp.Models;

/// <summary>
/// The server's response to a <c>resources/templates/list</c> request.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/resources#resource-templates.
/// </remarks>
public class McpListResourceTemplatesResultModel
{
    /// <summary>
    /// The list of resource templates. QueryCat does not provide templates yet.
    /// </summary>
    public List<JsonElement> ResourceTemplates { get; set; } = new();

    /// <summary>
    /// An opaque token representing the pagination cursor.
    /// </summary>
    public string? NextCursor { get; set; }
}
