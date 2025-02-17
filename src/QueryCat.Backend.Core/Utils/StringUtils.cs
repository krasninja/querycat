using System.Globalization;
using System.Text;

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// String utils.
/// </summary>
public static class StringUtils
{
    private const string QuoteChar = "\"";
    private static readonly SimpleObjectPool<StringBuilder> _stringBuilderPool = new(
        createFunc: () => new StringBuilder(),
        beforeReturn: sb => sb.Clear());

    /// <summary>
    /// Quote the specified string. If string doesn't contain the quote character
    /// the target string will be returned instead.
    /// </summary>
    /// <param name="target">Target string.</param>
    /// <param name="quote">Quote character.</param>
    /// <param name="force">Force quote.</param>
    /// <returns>Quoted string.</returns>
    public static ReadOnlySpan<char> Quote(
        ReadOnlySpan<char> target,
        string quote = QuoteChar,
        bool force = false)
    {
        // If already quoted - return the target string.
        if (target.IndexOf(quote) == 0 && target.LastIndexOf(quote) == target.Length - 1 && !force)
        {
            return target;
        }
        var sb = _stringBuilderPool.Get()
            .Append(quote)
            .Append(target)
            .Replace(quote, quote + quote, 1, target.Length)
            .Append(quote);
        var result = sb.ToString();
        _stringBuilderPool.Return(sb);
        return result;
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
        var sb = _stringBuilderPool.Get()
            .Append(target.Slice(1, target.Length - 2))
            .Replace(quoteChar + quoteChar, quoteChar);
        var result = sb.ToString();
        _stringBuilderPool.Return(sb);
        return result;
    }

    /// <summary>
    /// Get fields array from string using the specified delimiter.
    /// </summary>
    /// <param name="line">Target line to split.</param>
    /// <param name="delimiter">Delimiter.</param>
    /// <param name="quoteChar">Quote char.</param>
    /// <returns>Fields.</returns>
    /// <remarks>
    /// Source: https://www.codeproject.com/Tips/823670/Csharp-Light-and-Fast-CSV-Parser.
    /// </remarks>
    public static string[] GetFieldsFromLine(string line, char delimiter = ',', char quoteChar = '"')
    {
        var inQuote = false;
        var record = new List<string>();
        var sb = _stringBuilderPool.Get();
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
                if (readChar == quoteChar)
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
            else if (readChar == quoteChar)
            {
                if (inQuote)
                {
                    if ((char)reader.Peek() == quoteChar)
                    {
                        reader.Read();
                        sb.Append(quoteChar);
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

        _stringBuilderPool.Return(sb);
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
    internal static string SafeSubstring(string? target, int startIndex, int length = 0)
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
        if (text.StartsWith('\'') && text.EndsWith('\''))
        {
            return text.Substring(1, text.Length - 2).Replace("''", "'");
        }
        if (text.StartsWith('\"') && text.EndsWith('\"'))
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
    internal static string Unescape(string str)
    {
        if (str.IndexOf('\\') < 0)
        {
            return str;
        }

        var sb = _stringBuilderPool.Get();
        var escapeMode = false;
        // Based on https://github.com/coreutils/coreutils/blob/master/src/tr.c#L433 and
        // https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-STRINGS-ESCAPE.
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
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                    sb.Append(char.ConvertFromUtf32(GetOctetNumberInAdvance(ref i, str)));
                    break;
                case 'x':
                    i++;
                    sb.Append(char.ConvertFromUtf32(GetHexNumberInAdvance(ref i, str)));
                    break;
                case 'u':
                case 'U':
                    i++;
                    sb.Append(char.ConvertFromUtf32(GetHexNumberInAdvance(ref i, str)));
                    break;
            }

            escapeMode = false;
        }
        var result = sb.ToString();
        _stringBuilderPool.Return(sb);
        return result;
    }

    private static int GetOctetNumberInAdvance(ref int index, ReadOnlySpan<char> target)
    {
        var startIndex = index;
        for (; index < target.Length && char.IsBetween(target[index], '0', '7'); index++)
        {
        }
        var result = target.Slice(startIndex, index - startIndex);
        return Convert.ToInt32(result.ToString(), 8);
    }

    private static int GetHexNumberInAdvance(ref int index, ReadOnlySpan<char> target)
    {
        var startIndex = index;
        for (; index < target.Length && char.IsAsciiHexDigit(target[index]); index++)
        {
        }
        var result = target.Slice(startIndex, index - startIndex);
        return int.Parse(result, NumberStyles.HexNumber);
    }
}
