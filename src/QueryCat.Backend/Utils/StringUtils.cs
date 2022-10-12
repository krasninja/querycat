using System.Buffers;
using System.Text;

namespace QueryCat.Backend.Utils;

/// <summary>
/// String utils.
/// </summary>
internal static class StringUtils
{
    /// <summary>
    /// Implements SQL LIKE pattern comparision.
    /// </summary>
    /// <param name="pattern">"Like" pattern.</param>
    /// <param name="str">Target string.</param>
    /// <returns><c>True</c> if the target string matches the pattern, <c>false</c> otherwise.</returns>
    public static bool MatchesToLikePattern(ReadOnlySpan<char> pattern, ReadOnlySpan<char> str)
    {
        // Based on this: https://stackoverflow.com/questions/5417070/c-sharp-version-of-sql-like/8583383#8583383
        bool isMatch = true,
            isWildCardOn = false,
            isCharWildCardOn = false,
            isCharSetOn = false,
            isNotCharSetOn = false,
            endOfPattern = false;
        int lastWildCard = -1;
        int patternIndex = 0;
        var set = new List<char>();
        char p = '\0';

        for (int i = 0; i < str.Length; i++)
        {
            var c = str[i];
            endOfPattern = patternIndex >= pattern.Length;
            if (!endOfPattern)
            {
                p = pattern[patternIndex];

                if (!isWildCardOn && p == '%')
                {
                    lastWildCard = patternIndex;
                    isWildCardOn = true;
                    while (patternIndex < pattern.Length
                        && pattern[patternIndex] == '%')
                    {
                        patternIndex++;
                    }
                    if (patternIndex >= pattern.Length)
                    {
                        p = '\0';
                    }
                    else
                    {
                        p = pattern[patternIndex];
                    }
                }
                else if (p == '_')
                {
                    isCharWildCardOn = true;
                    patternIndex++;
                }
                else if (p == '[')
                {
                    if (pattern[++patternIndex] == '^')
                    {
                        isNotCharSetOn = true;
                        patternIndex++;
                    }
                    else
                    {
                        isCharSetOn = true;
                    }

                    set.Clear();
                    if (pattern[patternIndex + 1] == '-' && pattern[patternIndex + 3] == ']')
                    {
                        var start = char.ToUpper(pattern[patternIndex]);
                        patternIndex += 2;
                        var end = char.ToUpper(pattern[patternIndex]);
                        if (start <= end)
                        {
                            for (char ci = start; ci <= end; ci++)
                            {
                                set.Add(ci);
                            }
                        }
                        patternIndex++;
                    }

                    while (patternIndex < pattern.Length
                           && pattern[patternIndex] != ']')
                    {
                        set.Add(pattern[patternIndex]);
                        patternIndex++;
                    }
                    patternIndex++;
                }
            }

            if (isWildCardOn)
            {
                if (char.ToUpper(c) == char.ToUpper(p))
                {
                    isWildCardOn = false;
                    patternIndex++;
                }
            }
            else if (isCharWildCardOn)
            {
                isCharWildCardOn = false;
            }
            else if (isCharSetOn || isNotCharSetOn)
            {
                bool charMatch = set.Contains(char.ToUpper(c));
                if ((isNotCharSetOn && charMatch) || (isCharSetOn && !charMatch))
                {
                    if (lastWildCard >= 0)
                    {
                        patternIndex = lastWildCard;
                    }
                    else
                    {
                        isMatch = false;
                        break;
                    }
                }
                isNotCharSetOn = isCharSetOn = false;
            }
            else
            {
                if (char.ToUpper(c) == char.ToUpper(p))
                {
                    patternIndex++;
                }
                else
                {
                    if (lastWildCard >= 0)
                    {
                        patternIndex = lastWildCard;
                    }
                    else
                    {
                        isMatch = false;
                        break;
                    }
                }
            }
        }
        endOfPattern = patternIndex >= pattern.Length;

        if (isMatch && !endOfPattern)
        {
            bool isOnlyWildCards = true;
            for (int i = patternIndex; i < pattern.Length; i++)
            {
                if (pattern[i] != '%')
                {
                    isOnlyWildCards = false;
                    break;
                }
            }
            if (isOnlyWildCards)
            {
                endOfPattern = true;
            }
        }
        return isMatch && endOfPattern;
    }

    /// <summary>
    /// Unquote the target string.
    /// </summary>
    /// <param name="target">Target string.</param>
    /// <param name="quoteChar">Quote character.</param>
    /// <returns>Unquoted string.</returns>
    public static ReadOnlySpan<char> Unquote(ReadOnlySpan<char> target, char quoteChar = '"')
    {
        if (target.IsEmpty)
        {
            return ReadOnlySpan<char>.Empty;
        }
        int startIndex = 0;
        int endIndex = target.Length;
        if (target[0] == quoteChar)
        {
            startIndex++;
            endIndex--;
        }

        // TODO: can be optimized by using buffer like method below.
        var sb = new StringBuilder(endIndex);
        for (var i = startIndex; i < endIndex; i++)
        {
            if (target[i] == quoteChar)
            {
                sb.Append(target[startIndex..i]);
                startIndex = ++i;
            }
        }
        sb.Append(target.Slice(startIndex, endIndex - startIndex));
        return sb.ToString();
    }

    /// <summary>
    /// Unquote the target string.
    /// </summary>
    /// <param name="target">Target string.</param>
    /// <param name="quoteChar">Quote character.</param>
    /// <returns>Unquoted string.</returns>
    public static ReadOnlySpan<char> Unquote(ReadOnlySequence<char> target, char quoteChar = '"')
    {
        if (target.IsEmpty)
        {
            return ReadOnlySpan<char>.Empty;
        }
        int endIndex = (int)target.Length;
        var valueReader = target.First.Span[0] == quoteChar
            ? new SequenceReader<char>(target.Slice(1, --endIndex - 1))
            : new SequenceReader<char>(target);

        var buffer = new char[endIndex + 1].AsSpan();
        var lastBufferIndex = 0;
        while (valueReader.TryReadTo(out ReadOnlySpan<char> span, quoteChar))
        {
            if (span.IsEmpty)
            {
                break;
            }
            span.CopyTo(buffer.Slice(lastBufferIndex, span.Length));
            lastBufferIndex += span.Length;
            buffer[lastBufferIndex++] = quoteChar;
            valueReader.Advance(1);
        }
        var unreadSequence = valueReader.UnreadSequence;
        var unreadSequenceLength = (int)unreadSequence.Length;
        unreadSequence.CopyTo(buffer.Slice(lastBufferIndex, unreadSequenceLength));
        lastBufferIndex += unreadSequenceLength;
        return buffer.Slice(0, lastBufferIndex);
    }
}
