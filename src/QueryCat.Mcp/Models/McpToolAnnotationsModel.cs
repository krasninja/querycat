namespace QueryCat.Mcp.Models;

/// <summary>
/// Optional annotations for the tool. The client can use annotations to inform how the tool is used
/// or displayed. Hints are non-binding — a client MAY use them to inform its behavior, but is not
/// required to do so.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#tool-annotations.
/// </remarks>
public class McpToolAnnotationsModel
{
    /// <summary>
    /// A human-readable title for the tool.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// If true, the tool does not modify its environment.
    /// Default: false.
    /// </summary>
    public bool? ReadOnlyHint { get; set; }

    /// <summary>
    /// If true, the tool may interact with an "open world" of external entities.
    /// Default: true.
    /// </summary>
    public bool? OpenWorldHint { get; set; }

    /// <summary>
    /// If true, the tool may perform destructive updates to its environment.
    /// If false, the tool only performs read-only side effects.
    /// Default: true.
    /// </summary>
    public bool? DestructiveHint { get; set; }

    /// <summary>
    /// If true, calling the tool repeatedly with the same arguments will have no additional effect
    /// on the its environment. Default: false.
    /// </summary>
    public bool? IdempotentHint { get; set; }
}
