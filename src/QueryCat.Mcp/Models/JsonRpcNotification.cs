using System.Text.Json;

namespace QueryCat.Mcp.Models;

/// <summary>
/// A notification sent by the server. It has no identifier and does not expect a response.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/schema#jsonrpcnotification.
/// </remarks>
public sealed class JsonRpcNotification : JsonRpcFrame
{
    /// <summary>
    /// Method name.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Method arguments.
    /// </summary>
    public Dictionary<string, JsonElement>? Params { get; set; }
}
