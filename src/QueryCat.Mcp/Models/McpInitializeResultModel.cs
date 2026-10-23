namespace QueryCat.Mcp.Models;

/// <summary>
/// Result of the initialize request returned by the server.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/basic/lifecycle#initialization.
/// </remarks>
public class McpInitializeResultModel
{
    /// <summary>
    /// The version of the Model Context Protocol that the server will use.
    /// </summary>
    public string ProtocolVersion { get; set; } = string.Empty;

    /// <summary>
    /// The capabilities of the server.
    /// </summary>
    public McpServerCapabilitiesModel? Capabilities { get; set; }

    /// <summary>
    /// Server implementation information.
    /// </summary>
    public McpImplementationModel? ServerInfo { get; set; }

    /// <summary>
    /// Optional instructions for the client.
    /// </summary>
    public string? Instructions { get; set; }
}
