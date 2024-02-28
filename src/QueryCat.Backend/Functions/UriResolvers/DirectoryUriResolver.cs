using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Functions.UriResolvers;

internal sealed class DirectoryUriResolver : IUriResolver
{
    /// <inheritdoc />
    public bool TryResolve(string uri, out string? functionName)
    {
        uri = IOFunctions.ResolveHomeDirectory(uri);
        if (Directory.Exists(uri))
        {
            functionName = "ls_dir";
            return true;
        }
        functionName = null;
        return false;
    }
}
