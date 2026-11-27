namespace QueryCat.Mcp.Models;

/// <summary>
/// Parameters of the "notifications/subscriptions/acknowledged" notification.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/basic/patterns/subscriptions#acknowledgment.
/// </remarks>
public sealed class McpSubscriptionsAcknowledgedParamsModel : McpNotificationParamsModel
{
    /// <summary>
    /// The subset of requested notification types the server agreed to honor.
    /// </summary>
    public McpSubscriptionFilterModel Notifications { get; set; } = new();
}
