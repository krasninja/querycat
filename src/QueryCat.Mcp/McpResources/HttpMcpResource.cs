using QueryCat.Mcp.Models;

namespace QueryCat.Mcp.McpResources;

/// <summary>
/// MCP resource that is fetched from a web (HTTP or HTTPS) URL.
/// </summary>
internal sealed class HttpMcpResource : IMcpResource
{
    // Created lazily, so the proxy configured on application start (HttpClient.DefaultProxy) is used.
    private static readonly Lazy<HttpClient> _httpClient = new(() => new HttpClient());

    private readonly Uri _uri;

    /// <inheritdoc />
    public McpResourceModel Metadata { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <param name="uri">Resource web URL.</param>
    public HttpMcpResource(string name, Uri uri)
    {
        _uri = uri;
        Metadata = new McpResourceModel(uri.AbsoluteUri, name)
        {
            Description = string.Format(Resources.Messages.McpHttpResourceDescription, uri.AbsoluteUri),
        };
    }

    /// <inheritdoc />
    public async Task<Stream> OpenAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.Value.GetAsync(_uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        try
        {
            response.EnsureSuccessStatusCode();
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!string.IsNullOrEmpty(mediaType))
            {
                Metadata.MimeType = mediaType;
            }
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}
