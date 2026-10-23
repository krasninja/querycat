namespace QueryCat.Mcp.Models;

/// <summary>
/// Text content returned from a tool call.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#textcontent.
/// </remarks>
public sealed class McpToolTextContentModel : McpToolContentModel
{
    /// <summary>
    /// The text content.
    /// </summary>
    public string Text { get; set; } = string.Empty;
}
