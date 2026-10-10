namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Allows to process the natural language requests.
/// </summary>
public interface IAnswerAgent
{
    /// <summary>
    /// Get an answer from agent based on question.
    /// </summary>
    /// <param name="request">Request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="ChatResponse" />.</returns>
    Task<ChatResponse> AskAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}
