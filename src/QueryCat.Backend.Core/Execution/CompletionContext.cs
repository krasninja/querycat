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
        List<ParserToken> lastTokens)
    {
        ExecutionThread = executionThread;
        Text = text;
        LastTokens = new ParserTokensList(lastTokens);
    }
}
