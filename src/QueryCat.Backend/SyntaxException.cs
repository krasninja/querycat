namespace QueryCat.Backend;

/// <summary>
/// The exception occurs on semantic error.
/// </summary>
[Serializable]
#pragma warning disable CA2229
public class SyntaxException : QueryCatException
#pragma warning restore CA2229
{
    public string Query { get; }

    public int Line { get; }

    public int Position { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="query">Query text.</param>
    /// <param name="line">The line number where error occurs.</param>
    /// <param name="position">The character position where error occurs.</param>
    public SyntaxException(string message, string query, int line, int position) : base(message)
    {
        Query = query;
        Line = line;
        Position = position;
    }

    public string GetErrorLine()
    {
        using var reader = new StringReader(Query);
        int lineNum = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNum++;
            if (Line <= lineNum)
            {
                return line;
            }
        }
        return string.Empty;
    }
}
