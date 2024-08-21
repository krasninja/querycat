namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Completion context.
/// </summary>
public class CompletionContext
{
    /// <summary>
    /// Related execution thread.
    /// </summary>
    public IExecutionThread ExecutionThread { get; }

    /// <summary>
    /// Query text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Cursor position.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Last parsed tokens from the cursor position.
    /// </summary>
    public IReadOnlyList<ParserToken> LastTokens { get; }

    internal CompletionContext(IExecutionThread executionThread, string text, int position, IReadOnlyList<ParserToken> lastTokens)
    {
        ExecutionThread = executionThread;
        Text = text;
        Position = position;
        LastTokens = lastTokens;
    }
}
