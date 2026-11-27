namespace QueryCat.Mcp.Models;

/// <summary>
/// The server's response to a <c>server/discover</c> request.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/server/discover.
/// </remarks>
public sealed class McpDiscoverResultModel : McpCacheableResultModel
{
    /// <summary>
    /// Protocol versions the server supports.
    /// </summary>
    public List<string> SupportedVersions { get; set; } = new();

    /// <summary>
    /// The capabilities of the server.
    /// </summary>
    public McpServerCapabilitiesModel Capabilities { get; set; } = new();

    /// <summary>
    /// Optional instructions for the client.
    /// </summary>
    public string? Instructions { get; set; }
}
