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
    /// Send JSON RPC response to an agent. The request is identified by <see cref="JsonRpcFrame.SpanId" />.
    /// The response without result (an empty <see cref="JsonRpcResponse" />) must not be sent as a JSON-RPC message:
    /// for a notification it is an acknowledgement, for a request with an open notification stream
    /// (see <see cref="SendNotificationAsync" />) it means the stream stays open.
    /// </summary>
    /// <param name="response">JSON RPC response (success or error).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task SendResponseAsync(JsonRpcFrame response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send JSON RPC notification related to the in-flight request (for example, to the long-lived
    /// "subscriptions/listen" request). The transport may stream it on the request's response stream.
    /// </summary>
    /// <param name="notification">JSON RPC notification.</param>
    /// <param name="spanId">The request span id (see <see cref="JsonRpcFrame.SpanId" />). <see cref="Guid.Empty" />
    /// means the unsolicited notification for the clients that use legacy protocol versions
    /// (2025-11-25 and earlier), it is sent to all of them.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>True</c> if the notification has been sent, <c>false</c> if the request stream
    /// is closed (the client has disconnected).</returns>
    Task<bool> SendNotificationAsync(JsonRpcNotification notification, Guid spanId, CancellationToken cancellationToken = default);
}
