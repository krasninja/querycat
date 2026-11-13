using QueryCat.Backend.Core;

namespace QueryCat.Mcp.McpResources;

/// <summary>
/// Creates MCP resources from string definitions.
/// </summary>
public static class McpResourceFactory
{
    /// <summary>
    /// Create the resource from the definition in format "name=value", where value is a file path,
    /// "file://" URI or "http(s)://" URL.
    /// </summary>
    /// <param name="definition">Resource definition.</param>
    /// <returns>Instance of <see cref="IMcpResource" />.</returns>
    public static IMcpResource CreateFromDefinition(string definition)
    {
        var separatorIndex = definition.IndexOf('=');
        if (separatorIndex < 1 || separatorIndex == definition.Length - 1)
        {
            throw new QueryCatException(string.Format(Resources.Errors.McpInvalidResourceDefinition, definition));
        }
        var name = definition[..separatorIndex].Trim();
        var value = definition[(separatorIndex + 1)..].Trim();
        if (name.Length == 0 || value.Length == 0)
        {
            throw new QueryCatException(string.Format(Resources.Errors.McpInvalidResourceDefinition, definition));
        }
        return Create(name, value);
    }

    /// <summary>
    /// Create the resource.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <param name="value">File path, "file://" URI or "http(s)://" URL.</param>
    /// <returns>Instance of <see cref="IMcpResource" />.</returns>
    public static IMcpResource Create(string name, string value)
    {
        // Only treat the value as URI if it has a scheme, so Windows paths like "C:\file.txt" are files.
        if (value.Contains("://", StringComparison.Ordinal)
            && Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            if (uri.IsFile)
            {
                return new FileMcpResource(name, uri.LocalPath);
            }
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                return new HttpMcpResource(name, uri);
            }
            throw new QueryCatException(string.Format(Resources.Errors.McpUnsupportedResourceScheme, uri.Scheme));
        }
        return new FileMcpResource(name, value);
    }
}
