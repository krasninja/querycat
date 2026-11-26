namespace QueryCat.Mcp.Models;

/// <summary>
/// The server's response to a <c>resources/read</c> request.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/server/resources#reading-resources.
/// </remarks>
public class McpReadResourceResultModel : McpCacheableResultModel
{
    /// <summary>
    /// The resource contents.
    /// </summary>
    public List<McpResourceContentsModel> Contents { get; set; } = new();
}
