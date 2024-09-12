namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// The class describes the completion action to update the source text.
/// </summary>
public readonly struct CompletionTextEdit
{
    /// <summary>
    /// The range of the text document to be manipulated. Start position.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// The range of the text document to be manipulated. End position.
    /// </summary>
    public int End { get; }

    /// <summary>
    /// The string to be inserted. For delete operations use an empty string.
    /// </summary>
    public string NewText { get; }

    /// <summary>
    /// Text edit completion length to replace.
    /// </summary>
    public int ReplaceLength => End - Start;

    public CompletionTextEdit(int start, int end, string newText)
    {
        Start = end > -1 ? start : start + end;
        End = end > -1 ? end : start;

        if (Start > End)
        {
            (Start, End) = (End, Start);
        }

        NewText = newText;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Start}..{End}: {NewText}";
}
