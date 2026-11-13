using QueryCat.Backend.Core;
using QueryCat.Mcp.Models;

namespace QueryCat.Mcp.McpResources;

/// <summary>
/// MCP resource that is a local file.
/// </summary>
internal sealed class FileMcpResource : IMcpResource
{
    private readonly string _path;

    /// <inheritdoc />
    public McpResourceModel Metadata
    {
        get
        {
            var fileInfo = new FileInfo(_path);
            field.Size = fileInfo.Exists ? fileInfo.Length : null;
            return field;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <param name="path">File path.</param>
    public FileMcpResource(string name, string path)
    {
        _path = Path.GetFullPath(path);
        if (!File.Exists(_path))
        {
            throw new QueryCatException(string.Format(Resources.Errors.McpResourceFileNotFound, _path));
        }
        Metadata = new McpResourceModel(new Uri(_path).AbsoluteUri, name)
        {
            Description = string.Format(Resources.Messages.McpFileResourceDescription, _path),
        };
    }

    /// <inheritdoc />
    public Task<Stream> OpenAsync(CancellationToken cancellationToken)
    {
        Stream stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Task.FromResult(stream);
    }
}
