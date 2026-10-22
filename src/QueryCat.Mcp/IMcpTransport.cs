using QueryCat.Mcp.Models;

namespace QueryCat.Mcp;

/// <summary>
/// MCP transport abstraction.
/// </summary>
public interface IMcpTransport
{
    /// <summary>
    /// Start transport protocol handle.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stop transport protocol handle.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Await and get the next request from an agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON RPC request.</returns>
    Task<JsonRpcRequest?> GetNextRequestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send JSON RPC response to an agent.
    /// </summary>
    /// <param name="response">JSON RPC response (success or error).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task SendResponseAsync(JsonRpcFrame response, CancellationToken cancellationToken = default);
}
