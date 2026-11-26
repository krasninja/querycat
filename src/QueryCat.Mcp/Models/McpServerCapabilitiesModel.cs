namespace QueryCat.Mcp.Models;

/// <summary>
/// Server capabilities (what the server supports).
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/schema#servercapabilities.
/// </remarks>
public class McpServerCapabilitiesModel
{
    /// <summary>
    /// Tools capabilities. If present, the server offers tools.
    /// </summary>
    public McpServerToolsCapabilityModel? Tools { get; set; }

    /// <summary>
    /// Resources capabilities. If present, the server offers resources.
    /// </summary>
    public McpServerResourcesCapabilityModel? Resources { get; set; }
}
