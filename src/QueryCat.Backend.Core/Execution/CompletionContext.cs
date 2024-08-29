namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Completion context.
/// </summary>
public sealed class CompletionContext
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
    public ParserTokensList LastTokens { get; }

    /// <summary>
    /// Custom user data.
    /// </summary>
    public object? Tag { get; internal set; }

    internal CompletionContext(
        IExecutionThread executionThread,
        string text,
        int position,
        List<ParserToken> lastTokens)
    {
        ExecutionThread = executionThread;
        Text = text;
        Position = position;
        LastTokens = new ParserTokensList(lastTokens);
    }
}
