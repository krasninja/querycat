using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Object functions.
/// </summary>
internal static class ObjectFunctions
{
    [SafeFunction]
    [Description("Extracts a scalar value from a POCO .NET object.")]
    [FunctionSignature("object_query(obj: void, query: string): string")]
    public static VariantValue ObjectQuery(FunctionCallInfo args)
    {
        var obj = args.GetAt(0).AsObject;
        var query = args.GetAt(1).AsString;
        var value = Query(obj, query);
        return VariantValue.CreateFromObject(value);
    }

    private enum TokenType
    {
        Start,
        GetProperty,
        GetIndexed,
        End,
    }

    private readonly struct Token(TokenType type, int startIndex = -1, int length = 0)
    {
        public TokenType Type => type;

        public int StartIndex => startIndex;

        public int Length => length;
    }

    /// <summary>
    /// Query object.
    /// </summary>
    /// <param name="obj">Object to query.</param>
    /// <param name="query">Query string.</param>
    /// <returns>Scalar value.</returns>
    internal static object? Query(object? obj, ReadOnlySpan<char> query)
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

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(ObjectQuery);
    }
}
