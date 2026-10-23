namespace QueryCat.Mcp.Models;

/// <summary>
/// Server capabilities (what the server supports).
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/utilization#capabilities.
/// </remarks>
public class McpServerCapabilitiesModel
{
    /// <summary>
    /// Tools capabilities. If present, the server offers tools.
    /// </summary>
    public McpServerToolsCapabilityModel? Tools { get; set; }
}
