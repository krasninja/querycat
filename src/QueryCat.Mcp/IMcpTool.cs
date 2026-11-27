using System.Text.Json;
using QueryCat.Backend.Core.Execution;
using QueryCat.Mcp.Models;

namespace QueryCat.Mcp;

/// <summary>
/// MCP tool.
/// </summary>
public interface IMcpTool
{
    /// <summary>
    /// Tool description model.
    /// </summary>
    McpToolModel Metadata { get; }

    /// <summary>
    /// Call the tool.
    /// </summary>
    /// <param name="thread">Execution thread.</param>
    /// <param name="arguments">Tool arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="McpCallToolResultModel" />.</returns>
    Task<McpCallToolResultModel> CallAsync(
        IExecutionThread thread,
        IReadOnlyDictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken);
}
