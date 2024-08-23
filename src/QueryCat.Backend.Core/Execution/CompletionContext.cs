using System.Text;

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

    private readonly List<ParserToken> _lastTokens;

    /// <summary>
    /// Last parsed tokens from the cursor position.
    /// </summary>
    public IReadOnlyList<ParserToken> LastTokens => _lastTokens;

    /// <summary>
    /// Join token to a string.
    /// </summary>
    /// <param name="start">Start index.</param>
    /// <param name="length">How many token to join.</param>
    /// <returns>Joined tokens string.</returns>
    public string JoinTokens(int start, int length = -1)
    {
        var range = length > -1 ? _lastTokens[start..(start+length)] : _lastTokens[start..];
        var sb = new StringBuilder(capacity: range.Count * 13);
        foreach (var token in range)
        {
            sb.Append(token.Text);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Find token index based on predicate from the end.
    /// </summary>
    /// <param name="startIndex">Start index to find.</param>
    /// <param name="predicate">Predicate delegate.</param>
    /// <returns>Index on the found token or -1 if not found.</returns>
    public int FindLastTokenIndex(int startIndex, Predicate<ParserToken> predicate)
        => _lastTokens.FindLastIndex(startIndex, predicate);

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
        _lastTokens = lastTokens;
    }
}
