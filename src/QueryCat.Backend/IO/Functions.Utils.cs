using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.IO;

internal static partial class Functions
{
    public const string QueryDelimiter = "??";

    /// <summary>
    /// Split uri string to URI and arguments. For example: /tmp/1.json&amp;&amp;q=123 => /tmp/1.json, q=123.
    /// </summary>
    /// <param name="uri">URI string.</param>
    /// <returns>URI and arguments.</returns>
    public static (string Uri, FunctionCallArguments Args) Utils_ParseUri(string uri)
    {
        var delimiterIndex = uri.IndexOf(QueryDelimiter, StringComparison.Ordinal);
        if (delimiterIndex == -1)
        {
            return (uri, new FunctionCallArguments());
        }
        else
        {
            return (
                uri.Substring(0, delimiterIndex),
                FunctionCallArguments.FromQueryString(uri.Substring(delimiterIndex + QueryDelimiter.Length))
            );
        }
    }
}
