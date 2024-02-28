using System.Text;

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// String utils.
/// </summary>
internal static class StringUtils
{
    private const string QuoteChar = "\"";

    /// <summary>
    /// Quote the specified string. If string doesn't contain the quote character
    /// the target string will be returned instead.
    /// </summary>
    /// <param name="target">Target string.</param>
    /// <param name="quote">Quote character.</param>
    /// <param name="separator">The separator condition. If not exists - the string will not be quoted.</param>
    /// <param name="force">Force quote.</param>
    /// <returns>Quoted string.</returns>
    public static ReadOnlySpan<char> Quote(
        ReadOnlySpan<char> target,
        string quote = QuoteChar,
        char separator = ' ',
        bool force = false)
    {
        if (target.IndexOf(separator) == -1 && !force)
        {
            return target;
        }
        var sb = new StringBuilder(target.Length + 2)
            .Append(quote)
            .Append(target)
            .Replace(quote, quote + quote, 1, target.Length)
            .Append(quote);
        return sb.ToString();
    }

    /// <summary>
    /// Unquote the specified string.
    /// </summary>
    /// <param name="target">String to unquote.</param>
    /// <param name="quoteChar">Quote character.</param>
    /// <returns>Unquoted string.</returns>
    public static ReadOnlySpan<char> Unquote(ReadOnlySpan<char> target, string quoteChar = QuoteChar)
    {
        if (target.Length == 0 || target[..1].ToString() != quoteChar)
        {
            return target;
        }
        var sb = new StringBuilder(target.Length)
            .Append(target.Slice(1, target.Length - 2))
            .Replace(quoteChar + quoteChar, quoteChar);
        return sb.ToString();
    }

    /// <summary>
    /// Get fields array from string using the specified delimiter.
    /// </summary>
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
    public static string GetUnwrappedText(string text)
    {
        if (text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("]", StringComparison.Ordinal))
        {
            return text.Substring(1, text.Length - 2);
        }
        if (text.StartsWith("\'", StringComparison.Ordinal) && text.EndsWith("\'", StringComparison.Ordinal))
        {
            return text.Substring(1, text.Length - 2).Replace("''", "'");
        }
        if (text.StartsWith("\"", StringComparison.Ordinal) && text.EndsWith("\"", StringComparison.Ordinal))
        {
            return text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
        }
        return text;
    }

    /// <summary>
    /// Convert all \X escapes to their bytes representation.
    /// </summary>
    /// <param name="str">String pattern.</param>
    /// <returns>Converted string.</returns>
    public static string Unescape(string str)
    {
        if (str.IndexOf('\\') < 0)
        {
            return str;
        }

        var sb = new StringBuilder(str.Length);
        var escapeMode = false;
        // Based on https://github.com/coreutils/coreutils/blob/master/src/tr.c#L433 .
        for (var i = 0; i < str.Length; i++)
        {
            var ch = str[i];
            if (ch == '\\')
            {
                escapeMode = true;
                continue;
            }

            if (!escapeMode)
            {
                sb.Append(ch);
                continue;
            }

            switch (ch)
            {
                case '\\':
                    sb.Append('\\');
                    break;
                case 'a':
                    sb.Append('\u0007');
                    break;
                case 'b':
                    sb.Append('\b');
                    break;
                case 'e':
                    sb.Append('\u001B');
                    break;
                case 'f':
                    sb.Append('\f');
                    break;
                case 'n':
                    sb.Append('\n');
                    break;
                case 'r':
                    sb.Append('\r');
                    break;
                case 't':
                    sb.Append('\t');
                    break;
                case 'v':
                    sb.Append('\u000B');
                    break;
                // Not implemented yet cases.
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                    break;
                case 'x':
                    break;
                case 'u':
                    break;
            }

            escapeMode = false;
        }
        return sb.ToString();
    }
}
