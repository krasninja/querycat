using QueryCat.Mcp.Tools;

namespace QueryCat.Mcp;

/// <summary>
/// Prepares the instance of <see cref="McpServer" />.
/// </summary>
public static class McpServerInitializer
{
    /// <summary>
    /// Add default QueryCat tools.
    /// </summary>
    /// <param name="mcpServer">Instance of <see cref="McpServer" />.</param>
    public static void AddDefaultTools(McpServer mcpServer)
    {
        mcpServer.AddTool(new SearchFunctionsTool());
        mcpServer.AddTool(new CallFunctionTool());
        mcpServer.AddTool(new ExecuteQueryTool());
    }
}
