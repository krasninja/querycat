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

    /// <summary>
    /// Do not load plugins with the same name to avoid double loading.
    /// </summary>
    public bool SkipDuplicates { get; set; } = true;
}
