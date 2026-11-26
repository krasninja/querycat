using System.Text.Json.Serialization;

namespace QueryCat.Mcp.Models;

/// <summary>
/// Common parameters of the server notifications.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/schema#notificationparams.
/// </remarks>
[JsonDerivedType(typeof(McpSubscriptionsAcknowledgedParamsModel))]
public class McpNotificationParamsModel
{
    /// <summary>
    /// Protocol-level metadata.
    /// </summary>
    [JsonPropertyName("_meta")]
    public McpMetaModel? Meta { get; set; }
}
