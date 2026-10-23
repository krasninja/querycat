namespace QueryCat.Mcp.Models;

/// <summary>
/// The server's response to a <c>tools/list</c> request.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#listing-tools.
/// </remarks>
public class McpListToolsResultModel
{
    /// <summary>
    /// The list of tools offered by the server.
    /// </summary>
    public List<McpToolModel> Tools { get; set; } = new();

    /// <summary>
    /// An opaque token representing the pagination cursor. If present, indicates there are more
    /// tools available to retrieve.
    /// </summary>
    public string? NextCursor { get; set; }
}
