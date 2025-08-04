namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Question request.
/// </summary>
public class QuestionRequest
{
    public const string TypeGeneral = "general";
    public const string TypeSql = "sql";
    public const string TypeImage = "image-analysis";

    /// <summary>
    /// Question, issue or clarification text.
    /// </summary>
    public QuestionMessage[] Messages { get; }

    /// <summary>
    /// The whole message, sum of messages.
    /// </summary>
    public string Message => string.Join('\n', Messages.Select(m => m.Content));

    /// <summary>
    /// Specifies the request type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="text">Question text.</param>
    /// <param name="type">Question type.</param>
    public QuestionRequest(string text, string? type = null)
        : this([new QuestionMessage(text)], type)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="messages">Messages.</param>
    /// <param name="type">Question type.</param>
    public QuestionRequest(QuestionMessage[] messages, string? type = null)
    {
        Messages = messages;
        Type = type ?? TypeGeneral;
    }
}
