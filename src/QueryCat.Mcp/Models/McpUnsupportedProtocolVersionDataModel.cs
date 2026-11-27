namespace QueryCat.Mcp.Models;

/// <summary>
/// The data of the UnsupportedProtocolVersion error.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/schema#unsupportedprotocolversionerror.
/// </remarks>
public sealed class McpUnsupportedProtocolVersionDataModel
{
    /// <summary>
    /// Protocol versions the server supports.
    /// </summary>
    public List<string> Supported { get; set; } = new();

    /// <summary>
    /// The protocol version requested by the client.
    /// </summary>
    public string Requested { get; set; } = string.Empty;
}
