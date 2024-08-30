using System.Text;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Completion result: items and actions to apply.
/// </summary>
public class CompletionResult
{
    /// <summary>
    /// Empty completion result.
    /// </summary>
    public static CompletionResult Empty { get; } = new(Completion.Empty, []);

    /// <summary>
    /// Completion main data.
    /// </summary>
    public Completion Completion { get; }

    /// <summary>
    /// The text edits to be applied.
    /// </summary>
    public CompletionTextEdit[] Edits { get; }

    public CompletionResult(Completion completion, CompletionTextEdit[] edits)
    {
        Completion = completion;
        Edits = edits;
    }

    /// <summary>
    /// Apply text edits from the completion.
    /// </summary>
    /// <param name="text">Target string.</param>
    /// <returns>New replace string.</returns>
    public string Apply(string text)
    {
        var sb = new StringBuilder(text);
        Apply(sb);
        return sb.ToString();
    }

    /// <summary>
    /// Apply text edits from the completion.
    /// </summary>
    /// <param name="sb">Target instance of <see cref="StringBuilder" />.</param>
    public void Apply(StringBuilder sb)
    {
        foreach (var edit in Edits)
        {
            sb.Remove(edit.Start, edit.ReplaceLength);
            sb.Insert(edit.Start, edit.NewText);
        }
    }
}
