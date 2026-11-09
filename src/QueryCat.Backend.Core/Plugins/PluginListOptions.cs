namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Options for <see cref="IPluginsManager.ListAsync" />.
/// </summary>
public sealed class PluginListOptions
{
    /// <summary>
    /// Return only local installed plugins.
    /// </summary>
    public bool LocalOnly { get; set; }

    /// <summary>
    /// Search plugin pattern. Plugins which names contain the pattern
    /// (case-insensitive) are returned. Empty string - return all plugins.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Exact plugin name to find. Matches the full name or the short name
    /// without the known prefix ("github" -> "QueryCat.Plugins.Github").
    /// Empty string - do not filter by name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
