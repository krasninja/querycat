using System.Text;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Chat request to AI agent.
/// </summary>
public class ChatRequest
{
    public const string TypeGeneral = "general";
    public const string TypeSql = "sql";
    public const string TypeImage = "image-analysis";

    /// <summary>
    /// Question, issue or clarification text.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages { get; }

    /// <summary>
    /// The whole message, sum of messages.
    /// </summary>
    public string Message => string.Join('\n', Messages.Select(m => m.Content));

    /// <summary>
    /// Specifies the request type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Optional model identifier.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Optional tools available for the agent to call.
    /// </summary>
    public IReadOnlyList<ChatTool>? Tools { get; set; }

    /// <summary>
    /// Optional additional request options.
    /// </summary>
    public IDictionary<string, VariantValue>? Options { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="text">Question text.</param>
    /// <param name="type">Question type.</param>
    public ChatRequest(string text, string? type = null)
        : this([new ChatMessage(text)], type)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="messages">Messages.</param>
    /// <param name="type">Question type.</param>
    public ChatRequest(IReadOnlyList<ChatMessage> messages, string? type = null)
    {
        Messages = messages;
        Type = type ?? TypeGeneral;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Type: " + Type);
        if (!string.IsNullOrEmpty(Model))
        {
            sb.AppendLine("Model: " + Model);
        }
        if (Tools != null && Tools.Count > 0)
        {
            sb.AppendLine("Tools: " + string.Join(", ", Tools.Select(t => t.Name)));
        }
        foreach (var message in Messages)
        {
            sb.AppendLine(message.ToString());
        }
        return sb.ToString();
    }
}
