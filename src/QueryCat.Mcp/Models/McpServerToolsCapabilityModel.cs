namespace QueryCat.Mcp.Models;

/// <summary>
/// Server tools capability. Indicates whether the server offers tools and whether it supports
/// notifications for tool list changes.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#capabilities.
/// </remarks>
public class McpServerToolsCapabilityModel
{
    /// <summary>
    /// Whether this server supports notifications for changes to the tool list.
    /// </summary>
    public bool? ListChanged { get; set; }
}
