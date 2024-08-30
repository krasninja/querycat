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
    /// Query text and caret position.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Caret position.
    /// </summary>
    public int CaretPosition { get; }

    /// <summary>
    /// The start index position of trigger statement tokens.
    /// </summary>
    public int TriggerTokenPosition => TriggerTokens.Count > 0 ? TriggerTokens[0].StartIndex : 0;

    /// <summary>
    /// Current edit statement tokens.
    /// </summary>
    public ParserTokensList Tokens { get; }

    /// <summary>
    /// Current edit statement tokens.
    /// </summary>
    public ParserTokensList TriggerTokens { get; }

    /// <summary>
    /// Custom user data.
    /// </summary>
    public object? Tag { get; internal set; }

    internal CompletionContext(
        IExecutionThread executionThread,
        string text,
        IReadOnlyList<ParserToken> tokens,
        int caretPosition)
    {
        var tokensList = new ParserTokensList(tokens);

        ExecutionThread = executionThread;
        Text = text;
        CaretPosition = caretPosition;
        Tokens = new ParserTokensList(tokens);
        TriggerTokens = tokensList.GetRange(tokensList.FindLastIndex(t => t.IsSeparator()) + 1);
    }
}
