namespace QueryCat.Mcp.Models;

/// <summary>
/// The server's response to a <c>resources/read</c> request.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/resources#reading-resources.
/// </remarks>
public class McpReadResourceResultModel
{
    /// <summary>
    /// The resource contents.
    /// </summary>
    public List<McpResourceContentsModel> Contents { get; set; } = new();
}
