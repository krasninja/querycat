using System.Text;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Question chat message.
/// </summary>
public class QuestionMessage
{
    public const string RoleUser = "user";
    public const string RoleSystem = "system";
    public const string RoleAssistant = "assistant";
    public const string RoleTool = "tool";

    /// <summary>
    /// Empty message.
    /// </summary>
    public static QuestionMessage Empty { get; } = new(string.Empty);

    /// <summary>
    /// Message content.
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// Advises how to treat the message content.
    /// </summary>
    public string Role { get; private set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="content">Content.</param>
    /// <param name="role">Message role.</param>
    public QuestionMessage(string content, string? role = null)
    {
        Content = content;
        Role = role ?? RoleUser;
    }

    /// <summary>
    /// Merge content of several messages.
    /// </summary>
    /// <param name="messages">Messages to merge.</param>
    /// <returns>Instance of <see cref="QuestionMessage" />.</returns>
    public static QuestionMessage Merge(params IReadOnlyList<QuestionMessage> messages)
    {
        if (messages.Count == 0)
        {
            return new QuestionMessage(string.Empty);
        }

        var sb = new StringBuilder();
        var role = messages[0].Role;
        foreach (var message in messages)
        {
            sb.AppendLine(message.Content);
        }
        return new QuestionMessage(sb.ToString(), role);
    }

    /// <inheritdoc />
    public override string ToString() => $"({Role}): {Content}";
}
