namespace QueryCat.Mcp;

/// <summary>
/// MCP server options.
/// </summary>
public class McpServerOptions
{
    /// <summary>
    /// HTTP endpoint URL to listen on. If not set, the default one is used.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Additional allowed value of the HTTP "Origin" header. Loopback origins are always allowed,
    /// "*" allows any origin.
    /// </summary>
    public string? AllowOrigin { get; set; }
}
