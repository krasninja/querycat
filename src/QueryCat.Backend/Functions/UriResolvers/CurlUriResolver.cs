using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Functions.UriResolvers;

internal sealed class CurlUriResolver : IUriResolver
{
    /// <inheritdoc />
    public bool TryResolve(string uri, out string? functionDelegate)
    {
        if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            functionDelegate = "curl";
            return true;
        }
        functionDelegate = null;
        return false;
    }
}
