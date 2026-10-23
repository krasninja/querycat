using System.Text.Json;

namespace QueryCat.Mcp.Models;

/// <summary>
/// Parameters for a <c>tools/call</c> request sent by the client.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#calling-tools.
/// </remarks>
public class McpCallToolRequestParamsModel
{
    /// <summary>
    /// The name of the tool to call.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Arguments to pass to the tool. The keys and values are defined by the tool's
    /// <see cref="McpToolModel.InputSchema" />.
    /// </summary>
    public Dictionary<string, JsonElement>? Arguments { get; set; }
}
