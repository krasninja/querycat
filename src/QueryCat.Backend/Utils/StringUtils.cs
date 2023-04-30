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
        var lastWildCard = -1;
        var patternIndex = 0;
        var set = new List<char>();
        char p = '\0';

        for (var i = 0; i < str.Length; i++)
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
                    p = patternIndex >= pattern.Length ? '\0' : pattern[patternIndex];
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

    private const string QuoteChar = "\"";

    public static ReadOnlySpan<char> Quote(ReadOnlySpan<char> target, char separator = ' ')
    {
        if (target.IndexOf(separator) == -1)
        {
            return target;
        }
        var sb = new StringBuilder(target.Length + 2)
            .Append(QuoteChar)
            .Append(target)
            .Replace(QuoteChar, QuoteChar + QuoteChar, 1, target.Length)
            .Append(QuoteChar);
        return sb.ToString();
    }

    public static ReadOnlySpan<char> Unquote(ReadOnlySpan<char> target)
    {
        if (target.Length == 0 || target[..1].ToString() != QuoteChar)
        {
            return target;
        }
        var sb = new StringBuilder(target.Length)
            .Append(target.Slice(1, target.Length - 2))
            .Replace(QuoteChar + QuoteChar, QuoteChar);
        return sb.ToString();
    }

    /// <remarks>
    /// Source: https://www.codeproject.com/Tips/823670/Csharp-Light-and-Fast-CSV-Parser.
    /// </remarks>
    public static string[] GetFieldsFromLine(string line, char delimiter = ',')
    {
        var inQuote = false;
        var record = new List<string>();
        var sb = new StringBuilder();
        var reader = new StringReader(line);

        while (reader.Peek() != -1)
        {
            var readChar = (char)reader.Read();

            if (readChar == '\n' || (readChar == '\r' && (char)reader.Peek() == '\n'))
            {
                // If it's a \r\n combo consume the \n part and throw it away.
                if (readChar == '\r')
                {
                    reader.Read();
                }

                if (inQuote)
                {
                    if (readChar == '\r')
                    {
                        sb.Append('\r');
                    }
                    sb.Append('\n');
                }
                else
                {
                    if (record.Count > 0 || sb.Length > 0)
                    {
                        record.Add(sb.ToString());
                        sb.Clear();
                    }
                }
            }
            else if (sb.Length == 0 && !inQuote)
            {
                if (readChar == '"')
                {
                    inQuote = true;
                }
                else if (readChar == delimiter)
                {
                    record.Add(sb.ToString());
                    sb.Clear();
                }
                else if (char.IsWhiteSpace(readChar))
                {
                    // Ignore leading whitespace.
                }
                else
                {
                    sb.Append(readChar);
                }
            }
            else if (readChar == delimiter)
            {
                if (inQuote)
                {
                    sb.Append(delimiter);
                }
                else
                {
                    record.Add(sb.ToString());
                    sb.Clear();
                }
            }
            else if (readChar == '"')
            {
                if (inQuote)
                {
                    if ((char)reader.Peek() == '"')
                    {
                        reader.Read();
                        sb.Append('"');
                    }
                    else
                    {
                        inQuote = false;
                    }
                }
                else
                {
                    sb.Append(readChar);
                }
            }
            else
            {
                sb.Append(readChar);
            }
        }

        if (record.Count > 0 || sb.Length > 0)
        {
            record.Add(sb.ToString());
        }

        return record.ToArray();
    }

    /// <summary>
    /// Retrieves a substring from this instance. If the start index has negative value it will be replaced
    /// to 0. If substring exceeds the length of the target string the end of the string will be returned. <c>null</c> will
    /// be converted to the empty string.
    /// </summary>
    /// <param name="target">Target string.</param>
    /// <param name="startIndex">The zero-based starting character position of a substring in this instance.</param>
    /// <param name="length">The number of characters in the substring.</param>
    /// <returns>Substring.</returns>
    public static string SafeSubstring(string? target, int startIndex, int length = 0)
    {
        if (target == null)
        {
            return string.Empty;
        }

        if (startIndex < 0)
        {
            startIndex = 0;
        }
        else if (startIndex >= target.Length)
        {
            return string.Empty;
        }
        if (length == 0)
        {
            length = target.Length;
        }
        if (startIndex + length > target.Length)
        {
            length = target.Length - startIndex;
        }
        return target.Substring(startIndex, length);
    }

    /// <summary>
    /// Unwrap text from quotes and square brackets.
    /// </summary>
    /// <param name="text">Text to unwrap.</param>
    /// <returns>Unwrapped text.</returns>
    internal static string GetUnwrappedText(string text)
    {
        if ((text.StartsWith("\'", StringComparison.Ordinal) && text.EndsWith("\'", StringComparison.Ordinal))
            || (text.StartsWith("\"", StringComparison.Ordinal) && text.EndsWith("\"", StringComparison.Ordinal))
            || (text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("]", StringComparison.Ordinal)))
        {
            return text.Substring(1, text.Length - 2);
        }
        return text;
    }
}
