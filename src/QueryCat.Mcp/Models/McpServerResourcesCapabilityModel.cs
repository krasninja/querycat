namespace QueryCat.Mcp.Models;

/// <summary>
/// Server resources capability. Indicates whether the server offers resources.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/resources#capabilities.
/// </remarks>
public class McpServerResourcesCapabilityModel
{
    /// <summary>
    /// Whether the client can subscribe to be notified of changes to individual resources.
    /// </summary>
    public bool? Subscribe { get; set; }

    /// <summary>
    /// Whether this server supports notifications for changes to the resource list.
    /// </summary>
    public bool? ListChanged { get; set; }
}
