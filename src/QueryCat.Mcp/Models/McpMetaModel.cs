using System.Text.Json.Serialization;

namespace QueryCat.Mcp.Models;

/// <summary>
/// Protocol-level metadata of the server results and notifications (the "_meta" field).
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/basic#_meta.
/// </remarks>
public sealed class McpMetaModel
{
    /// <summary>
    /// Server name and version.
    /// </summary>
    [JsonPropertyName("io.modelcontextprotocol/serverInfo")]
    public McpImplementationModel? ServerInfo { get; set; }

    /// <summary>
    /// The JSON-RPC id of the "subscriptions/listen" request the message relates to.
    /// </summary>
    [JsonPropertyName("io.modelcontextprotocol/subscriptionId")]
    public object? SubscriptionId { get; set; }
}
