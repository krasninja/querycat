namespace QueryCat.Backend.Utils;

/// <summary>
/// Class that allows to make simple C#-like queries to POCO.
/// </summary>
internal static class ObjectQuery
{
    private enum TokenType
    {
        Start,
        GetProperty,
        GetIndexed,
        End,
    }

    private readonly struct Token
    {
        public TokenType Type { get; }

        public int StartIndex { get; } = -1;

        public int Length { get; }

        public Token(TokenType type, int startIndex = -1, int length = 0)
        {
            Type = type;
            StartIndex = startIndex;
            Length = length;
        }
    }

    /// <summary>
    /// Query object.
    /// </summary>
    /// <param name="obj">Object to query.</param>
    /// <param name="query">Query string.</param>
    /// <returns>Scalar value.</returns>
    public static object? Query(object? obj, ReadOnlySpan<char> query)
    {
        if (obj == null)
        {
            return null;
        }

        var token = new Token(TokenType.Start);
        var currentObject = obj;
        while ((token = GetNextToken(query, token)).Type != TokenType.End)
        {
            var value = query.Slice(token.StartIndex, token.Length);

            // PropertyGet.
            if (token.Type == TokenType.GetProperty)
            {
                if (value.Equals("this", StringComparison.Ordinal))
                {
                    continue;
                }
                var propInfo = currentObject.GetType().GetProperty(value.ToString());
                if (propInfo == null)
                {
                    return null;
                }
                currentObject = propInfo.GetValue(currentObject);
            }

            if (currentObject == null)
            {
                break;
            }
        }

        return currentObject;
    }

    private static Token GetNextToken(ReadOnlySpan<char> query, in Token token)
    {
        var start = token.StartIndex + token.Length;
        if (query.Length - start == 0)
        {
            return new Token(TokenType.End);
        }
        var slicedQuery = query.Slice(start + 1);
        var nextLength = slicedQuery.IndexOfAny(".?[]");
        if (nextLength == -1)
        {
            nextLength = slicedQuery.Length;
        }
        var type = TokenType.GetProperty;
        return new Token(type, start + 1, nextLength);
     }
}
