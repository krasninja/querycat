using QueryCat.Backend.Functions;

namespace QueryCat.Mcp.Models;

/// <summary>
/// Resources allow servers to share data that provides context to language models,
/// such as files, database schemas, or application-specific information.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/resources#resource.
/// </remarks>
public class McpResourceModel
{
    /// <summary>
    /// Unique identifier for the resource.
    /// </summary>
    public string Uri { get; private set; }

    /// <summary>
    /// The name of the resource.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Optional human-readable name of the resource for display purposes.
    /// </summary>
    public string? Title { get; private set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional MIME type.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Optional size of the raw resource content in bytes.
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="uri">Resource URI.</param>
    /// <param name="name">Resource name.</param>
    /// <param name="title">Resource title.</param>
    public McpResourceModel(string uri, string name, string? title = null)
    {
        Uri = uri;
        Name = name;
        Title = title;

        // Use only the path part to skip query string and fragment.
        var path = System.Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri) ? parsedUri.AbsolutePath : uri;
        var extension = Path.GetExtension(path);
        if (!string.IsNullOrEmpty(extension)
            && IOFunctions.MimeTypesProvider.TryGetContentTypeByExtension(extension, out var mimeType))
        {
            MimeType = mimeType;
        }
    }
}
