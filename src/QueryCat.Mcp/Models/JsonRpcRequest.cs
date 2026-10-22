using System.Text.Json;
using System.Text.Json.Serialization;

namespace QueryCat.Mcp.Models;

/// <summary>
/// A request that expects a response, or a notification if it has no identifier.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/schema#jsonrpcrequest.
/// </remarks>
public sealed class JsonRpcRequest : JsonRpcFrame
{
    /// <summary>
    /// Method name.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Method arguments.
    /// </summary>
    public Dictionary<string, JsonElement>? Params { get; set; }

    /// <summary>
    /// The request is a notification, the receiver must not send a response.
    /// </summary>
    [JsonIgnore]
    public bool IsNotification => Id == null;
}
