namespace QueryCat.Mcp.Models;

/// <summary>
/// The server's response to a <c>resources/list</c> request.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/resources#listing-resources.
/// </remarks>
public class McpListResourcesResultModel
{
    /// <summary>
    /// The list of resources offered by the server.
    /// </summary>
    public List<McpResourceModel> Resources { get; set; } = new();

    /// <summary>
    /// An opaque token representing the pagination cursor. If present, indicates there are more
    /// resources available to retrieve.
    /// </summary>
    public string? NextCursor { get; set; }
}
