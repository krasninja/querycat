using System.Collections;
using System.Diagnostics;
using System.Text;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// List of tokens with the additional functionality.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
public sealed class ParserTokensList : IReadOnlyList<ParserToken>
{
    private readonly List<ParserToken> _tokens;

    /// <inheritdoc />
    public ParserToken this[int index] => _tokens[index];

    /// <inheritdoc />
    public int Count => _tokens.Count;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="tokens">Tokens list.</param>
    public ParserTokensList(List<ParserToken> tokens)
    {
        _tokens = tokens;
    }

    /// <summary>
    /// Join token to a string.
    /// </summary>
    /// <param name="start">Start index.</param>
    /// <param name="length">How many token to join.</param>
    /// <returns>Joined tokens string.</returns>
    public string Join(int start, int length = -1)
    {
        var range = length > -1 ? _tokens[start..(start+length)] : _tokens[start..];
        var sb = new StringBuilder(capacity: range.Count * 13);
        foreach (var token in range)
        {
            sb.Append(token.Text);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Searches for an element that matches the conditions.
    /// </summary>
    /// <param name="predicate">Condition delegate.</param>
    /// <returns>The zero-based index of the first occurrence, otherwise -1.</returns>
    public int FindIndex(Predicate<ParserToken> predicate) => _tokens.FindIndex(predicate);

    /// <summary>
    /// Searches for an element that matches the conditions.
    /// </summary>
    /// <param name="startIndex">The zero-based starting index of the search.</param>
    /// <param name="predicate">Condition delegate.</param>
    /// <returns>The zero-based index of the first occurrence, otherwise -1.</returns>
    public int FindIndex(int startIndex, Predicate<ParserToken> predicate) => _tokens.FindIndex(startIndex, predicate);

    /// <summary>
    /// Searches for an element that matches the specified token type.
    /// </summary>
    /// <param name="tokenType">Token type to search.</param>
    /// <returns>The zero-based index of the first occurrence, otherwise -1.</returns>
    public int FindIndex(string tokenType) => _tokens.FindIndex(t => t.Type == tokenType);

    /// <summary>
    /// Searches for an element that matches the specified token type.
    /// </summary>
    /// <param name="startIndex">The zero-based starting index of the search.</param>
    /// <param name="tokenType">Token type to search.</param>
    /// <returns>The zero-based index of the first occurrence, otherwise -1.</returns>
    public int FindIndex(int startIndex, string tokenType) => _tokens.FindIndex(startIndex, t => t.Type == tokenType);

    /// <summary>
    /// Searches for an element that matches the conditions defined by the specified predicate, and returns the last occurrence.
    /// </summary>
    /// <param name="predicate">Predicate delegate.</param>
    /// <returns>Index on the found token or -1 if not found.</returns>
    public int FindLastIndex(Predicate<ParserToken> predicate) => _tokens.FindLastIndex(predicate);

    /// <summary>
    /// Searches for an element that matches the conditions defined by the specified predicate, and returns the last occurrence.
    /// </summary>
    /// <param name="startIndex">The zero-based starting index of the search.</param>
    /// <param name="predicate">Predicate delegate.</param>
    /// <returns>Index on the found token or -1 if not found.</returns>
    public int FindLastIndex(int startIndex, Predicate<ParserToken> predicate) => _tokens.FindLastIndex(startIndex, predicate);

    /// <summary>
    /// Searches for an element that matches the token type, and returns the last occurrence.
    /// </summary>
    /// <param name="tokenType">Token to search.</param>
    /// <returns>Index on the found token or -1 if not found.</returns>
    public int FindLastIndex(string tokenType) => _tokens.FindLastIndex(t => t.Type == tokenType);

    /// <summary>
    /// Searches for an element that matches the token type, and returns the last occurrence.
    /// </summary>
    /// <param name="startIndex">The zero-based starting index of the search.</param>
    /// <param name="tokenType">Token to search.</param>
    /// <returns>Index on the found token or -1 if not found.</returns>
    public int FindLastIndex(int startIndex, string tokenType) => _tokens.FindLastIndex(startIndex, t => t.Type == tokenType);

    /// <summary>
    /// Gets the index position of the token within the query text.
    /// </summary>
    /// <param name="tokenIndex">Token index.</param>
    /// <returns>The index position of the token.</returns>
    public int GetQueryPosition(int tokenIndex)
    {
        var count = 0;
        for (var i = 0; i < tokenIndex; i++)
        {
            count += _tokens[i].Text.Length;
        }
        return count;
    }

    /// <summary>
    /// Creates a shallow copy of a range of elements in the source.
    /// </summary>
    /// <param name="index">The zero-based index where the range start.</param>
    /// <returns>The shallow copy of the range.</returns>
    public ParserTokensList GetRange(int index) => new(_tokens.GetRange(index, _tokens.Count - index));

    /// <summary>
    /// Creates a shallow copy of a range of elements in the source.
    /// </summary>
    /// <param name="index">The zero-based index where the range start.</param>
    /// <param name="count">The number of elements in the range.</param>
    /// <returns>The shallow copy of the range.</returns>
    public ParserTokensList GetRange(int index, int count) => new(_tokens.GetRange(index, count));

    /// <inheritdoc />
    public IEnumerator<ParserToken> GetEnumerator() => _tokens.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
