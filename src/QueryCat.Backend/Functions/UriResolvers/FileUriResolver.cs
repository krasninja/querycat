using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Functions.UriResolvers;

/// <summary>
/// Partial support of file URI scheme.
/// </summary>
/// <remarks>
/// https://en.wikipedia.org/wiki/File_URI_scheme.
/// </remarks>
internal sealed class FileUriResolver : IUriResolver
{
    /// <inheritdoc />
    public bool TryResolve(string uri, out string? functionName)
    {
        if (uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            functionName = "read_file";
            return true;
        }
        functionName = null;
        return false;
    }
}
