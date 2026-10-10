using System.Text;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Chat response from AI agent.
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// Message identifier, can be useful for debug.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// Response messages.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages { get; }

    /// <summary>
    /// Answer from resolver (content of the last message).
    /// </summary>
    public string Answer => Messages.Count > 0 ? Messages[^1].Content : string.Empty;

    /// <summary>
    /// Optional model identifier that produced the response.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Optional stop reason: "end", "tool_calls", "max_tokens".
    /// </summary>
    public string? StopReason { get; set; }

    /// <summary>
    /// Optional response metadata.
    /// </summary>
    public IDictionary<string, VariantValue>? Metadata { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="messages">Response messages.</param>
    /// <param name="messageId">Message identifier.</param>
    public ChatResponse(IReadOnlyList<ChatMessage> messages, string? messageId = null)
    {
        MessageId = messageId ?? string.Empty;
        Messages = messages;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="answer">Answer text.</param>
    /// <param name="messageId">Message identifier.</param>
    public ChatResponse(string answer, string? messageId = null)
        : this([new ChatMessage(answer, ChatMessage.RoleAssistant)], messageId)
    {
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(MessageId))
        {
            sb.Append(MessageId);
            sb.Append(": ");
        }
        sb.Append(Answer);
        return sb.ToString();
    }
}
