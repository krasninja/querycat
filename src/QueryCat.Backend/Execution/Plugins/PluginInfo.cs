namespace QueryCat.Backend.Execution.Plugins;

/// <summary>
/// Plugin information.
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// Plugin name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Full URI to plugin file.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Plugin version. It will be "0.0" if not defined.
    /// </summary>
    public Version Version { get; set; } = new();

    /// <summary>
    /// Is plugin installed and ready to use.
    /// </summary>
    public bool IsInstalled { get; set; }

    public PluginInfo(string name)
    {
        Name = name;
    }

    public bool IsEqualTo(PluginInfo pluginInfo)
    {
        return pluginInfo.Name == this.Name && pluginInfo.Version == this.Version;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name}-{Version}";
}
