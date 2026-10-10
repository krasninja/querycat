using System.Text;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// AI conversation chat message.
/// </summary>
public class ChatMessage
{
    public const string RoleUser = "user";
    public const string RoleSystem = "system";
    public const string RoleAssistant = "assistant";
    public const string RoleTool = "tool";

    /// <summary>
    /// Empty message.
    /// </summary>
    public static ChatMessage Empty { get; } = new(string.Empty);

    /// <summary>
    /// Message content.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Advises how to treat the message content.
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// Tool calls requested by the assistant (assistant role only).
    /// </summary>
    public IReadOnlyList<ChatToolCall> ToolCalls { get; }

    /// <summary>
    /// The tool call identifier this message is a response to (tool role only).
    /// </summary>
    public string ToolCallId { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="content">Content.</param>
    /// <param name="role">Message role.</param>
    /// <param name="toolCalls">Tool calls (assistant role only).</param>
    /// <param name="toolCallId">Tool call identifier (tool role only).</param>
    public ChatMessage(
        string content,
        string? role = null,
        IReadOnlyList<ChatToolCall>? toolCalls = null,
        string? toolCallId = null)
    {
        Content = content;
        Role = role ?? RoleUser;
        ToolCalls = toolCalls ?? [];
        ToolCallId = toolCallId ?? string.Empty;
    }

    /// <summary>
    /// Merge content of several messages.
    /// </summary>
    /// <param name="messages">Messages to merge.</param>
    /// <returns>Instance of <see cref="ChatMessage" />.</returns>
    public static ChatMessage Merge(params IReadOnlyList<ChatMessage> messages)
    {
        if (messages.Count == 0)
        {
            return new ChatMessage(string.Empty);
        }

        var sb = new StringBuilder();
        var role = messages[0].Role;
        foreach (var message in messages)
        {
            sb.AppendLine(message.Content);
        }
        return new ChatMessage(sb.ToString(), role);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (ToolCalls.Count > 0)
        {
            return $"({Role}): {Content} [tool_calls: {string.Join(", ", ToolCalls.Select(tc => tc.Name))}]";
        }
        if (!string.IsNullOrEmpty(ToolCallId))
        {
            return $"({Role}/{ToolCallId}): {Content}";
        }
        return $"({Role}): {Content}";
    }
}
