namespace QueryCat.Mcp.Models;

/// <summary>
/// A notification sent by the server. It has no identifier and does not expect a response.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/schema#jsonrpcnotification.
/// </remarks>
public sealed class JsonRpcNotification : JsonRpcFrame
{
    /// <summary>
    /// Method name.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Notification parameters.
    /// </summary>
    public McpNotificationParamsModel? Params { get; set; }
}
