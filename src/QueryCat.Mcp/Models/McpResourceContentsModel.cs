namespace QueryCat.Mcp.Models;

/// <summary>
/// The contents of a specific resource. Either <see cref="Text" /> or <see cref="Blob" /> is set.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/resources#resource-contents.
/// </remarks>
public class McpResourceContentsModel
{
    /// <summary>
    /// The URI of the resource.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// The MIME type of the resource.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// The text of the resource. Set only for text resources.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// A base64-encoded binary data of the resource. Set only for binary resources.
    /// </summary>
    public string? Blob { get; set; }
}
