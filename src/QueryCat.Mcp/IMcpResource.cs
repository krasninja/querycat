using QueryCat.Mcp.Models;

namespace QueryCat.Mcp;

/// <summary>
/// MCP resource.
/// </summary>
public interface IMcpResource
{
    /// <summary>
    /// Resource description model.
    /// </summary>
    McpResourceModel Metadata { get; }

    /// <summary>
    /// Open the resource content stream for reading. The caller must dispose the stream.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resource content stream.</returns>
    Task<Stream> OpenAsync(CancellationToken cancellationToken);
}
