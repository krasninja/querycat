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
    /// The title of the implementation.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The description of the implementation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The version of the implementation.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Implementation website.
    /// </summary>
    public string? WebsiteUrl { get; set; }
}
