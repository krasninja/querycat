namespace QueryCat.Mcp.Models;

/// <summary>
/// A link to a resource returned from a tool call.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#resourcelink.
/// </remarks>
public class McpToolResourceLinkModel : McpToolContentModel
{
    /// <summary>
    /// The URI of the resource.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// The name of the resource.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of the resource.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The MIME type of the resource.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// The size of the resource in bytes.
    /// </summary>
    public long? Size { get; set; }
}
