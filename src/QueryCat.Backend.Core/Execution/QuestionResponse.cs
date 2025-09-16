using System.Text;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Question response.
/// </summary>
public class QuestionResponse
{
    /// <summary>
    /// Message identifier, can be useful for debug.
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// Answer from resolver.
    /// </summary>
    public string Answer { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="answer">Query.</param>
    /// <param name="messageId">Message identifier.</param>
    public QuestionResponse(string answer, string? messageId = null)
    {
        MessageId = messageId ?? string.Empty;
        Answer = answer;
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
