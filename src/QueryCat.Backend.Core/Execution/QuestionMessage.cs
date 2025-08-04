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
    /// Message content.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Advises how to treat the message content.
    /// </summary>
    public string Role { get; }

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
}
