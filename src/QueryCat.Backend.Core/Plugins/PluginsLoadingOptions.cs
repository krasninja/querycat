namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Options for plugins loading.
/// </summary>
public sealed class PluginsLoadingOptions
{
    /// <summary>
    /// Load only the specified plugins by names. If empty - filter will not be enabled.
    /// </summary>
    public string[] Filter { get; set; } = [];
}
