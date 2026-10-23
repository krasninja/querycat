namespace QueryCat.Mcp.Models;

/// <summary>
/// Image content returned from a tool call.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#imagecontent.
/// </remarks>
public sealed class McpToolImageContentModel : McpToolContentModel
{
    /// <summary>
    /// The base64-encoded image data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// The MIME type of the image. Different providers may support different image types.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
}
