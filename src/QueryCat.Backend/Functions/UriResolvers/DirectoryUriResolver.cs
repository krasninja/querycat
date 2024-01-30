using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Functions.UriResolvers;

internal sealed class DirectoryUriResolver : IUriResolver
{
    /// <inheritdoc />
    public bool TryResolve(string uri, out string? functionDelegate)
    {
        uri = IOFunctions.ResolveHomeDirectory(uri);
        if (Directory.Exists(uri))
        {
            functionDelegate = "ls_dir";
            return true;
        }
        functionDelegate = null;
        return false;
    }
}
