using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Functions.UriResolvers;

internal sealed class CurlUriResolver : IUriResolver
{
    /// <inheritdoc />
    public bool TryResolve(string uri, out string? functionName)
    {
        if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            functionName = "curl";
            return true;
        }
        functionName = null;
        return false;
    }
}
