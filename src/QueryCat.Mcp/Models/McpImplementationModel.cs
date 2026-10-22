namespace QueryCat.Mcp.Models;

/// <summary>
/// Describes the name and version of a MCP implementation.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-03-26/basic/utilities#implementation.
/// </remarks>
public class McpImplementationModel
{
    /// <summary>
    /// The name of the implementation.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The version of the implementation.
    /// </summary>
    public string Version { get; set; } = string.Empty;
}
