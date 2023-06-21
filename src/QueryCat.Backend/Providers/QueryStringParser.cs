using QueryCat.Backend.Functions;

namespace QueryCat.Backend.Providers;

internal static class QueryStringParser
{
    public const string QueryDelimiter = "??";

    /// <summary>
    /// Split uri string to URI and arguments. For example: /tmp/1.json&&q=123 => /tmp/1.json, q=123.
    /// </summary>
    /// <param name="uri">URI string.</param>
    /// <returns>URI and arguments.</returns>
    public static (string Uri, FunctionArguments Args) ParseUri(string uri)
    {
        var delimiterIndex = uri.IndexOf(QueryDelimiter, StringComparison.Ordinal);
        if (delimiterIndex == -1)
        {
            return (uri, new FunctionArguments());
        }
        else
        {
            return (
                uri.Substring(0, delimiterIndex),
                FunctionArguments.FromQueryString(uri.Substring(delimiterIndex + QueryDelimiter.Length))
            );
        }
    }
}
