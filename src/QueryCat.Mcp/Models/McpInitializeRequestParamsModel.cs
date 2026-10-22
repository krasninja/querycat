namespace QueryCat.Mcp.Models;

/// <summary>
/// Parameters for the initialize request sent by the client.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-03-26/basic/lifecycle#initialization.
/// </remarks>
public class McpInitializeRequestParamsModel
{
    /// <summary>
    /// The latest version of the Model Context Protocol that the client supports.
    /// </summary>
    public string ProtocolVersion { get; set; } = string.Empty;

    /// <summary>
    /// The capabilities of the client.
    /// </summary>
    public McpClientCapabilitiesModel? Capabilities { get; set; }

    /// <summary>
    /// Client implementation information.
    /// </summary>
    public McpImplementationModel? ClientInfo { get; set; }
}
