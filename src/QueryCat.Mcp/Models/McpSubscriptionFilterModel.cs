namespace QueryCat.Mcp.Models;

/// <summary>
/// The notification types the client wants to receive on a <c>subscriptions/listen</c> stream.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/basic/patterns/subscriptions#notification-filter.
/// </remarks>
public sealed class McpSubscriptionFilterModel
{
    /// <summary>
    /// Receive "notifications/tools/list_changed" when tools change.
    /// </summary>
    public bool? ToolsListChanged { get; set; }

    /// <summary>
    /// Receive "notifications/prompts/list_changed" when prompts change.
    /// </summary>
    public bool? PromptsListChanged { get; set; }

    /// <summary>
    /// Receive "notifications/resources/list_changed" when the resource list changes.
    /// </summary>
    public bool? ResourcesListChanged { get; set; }

    /// <summary>
    /// Receive "notifications/resources/updated" for these resource URIs.
    /// </summary>
    public List<string>? ResourceSubscriptions { get; set; }
}
