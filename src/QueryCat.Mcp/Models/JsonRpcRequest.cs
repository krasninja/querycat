using System.Text.Json;
using System.Text.Json.Serialization;

namespace QueryCat.Mcp.Models;

/// <summary>
/// A request that expects a response, or a notification if it has no identifier.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/schema#jsonrpcrequest.
/// </remarks>
public sealed class JsonRpcRequest : JsonRpcFrame
{
    /// <summary>
    /// The name of the request metadata parameter.
    /// </summary>
    public const string MetaParam = "_meta";

    /// <summary>
    /// Protocol version for the request.
    /// </summary>
    public const string MetaProtocolVersion = "io.modelcontextprotocol/protocolVersion";

    /// <summary>
    /// Client capabilities relevant to the request.
    /// </summary>
    public const string MetaClientCapabilities = "io.modelcontextprotocol/clientCapabilities";

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

    /// <summary>
    /// The transport hint whether the request uses a legacy (initialization-based, 2025-11-25 and earlier)
    /// protocol version. <c>Null</c> if the transport cannot determine it (the server decides by its own state).
    /// </summary>
    [JsonIgnore]
    public bool? IsLegacy { get; set; }

    /// <summary>
    /// Get the string parameter value.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <returns>The value or <c>null</c> if it is not found or not a string.</returns>
    public string? GetStringParam(string name)
    {
        if (Params != null
            && Params.TryGetValue(name, out var element)
            && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }
        return null;
    }

    /// <summary>
    /// Get the request metadata value ("_meta" parameter) by the key.
    /// </summary>
    /// <param name="key">Metadata key.</param>
    /// <returns>The value or <c>null</c> if it is not found.</returns>
    public JsonElement? GetMeta(string key)
    {
        if (Params != null
            && Params.TryGetValue(MetaParam, out var metaElement)
            && metaElement.ValueKind == JsonValueKind.Object
            && metaElement.TryGetProperty(key, out var value))
        {
            return value;
        }
        return null;
    }

    /// <summary>
    /// Get the protocol version from the request metadata.
    /// </summary>
    /// <returns>Protocol version or <c>null</c> if it is not specified.</returns>
    public string? GetProtocolVersion()
    {
        var value = GetMeta(MetaProtocolVersion);
        return value is { ValueKind: JsonValueKind.String } ? value.Value.GetString() : null;
    }
}
