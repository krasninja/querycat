namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Registry of configuration options declared by plugins.
/// </summary>
public interface IPluginConfigOptionsRegistry
{
    /// <summary>
    /// Register configuration option. If the plugin already has the option with the same name, it is replaced.
    /// </summary>
    /// <param name="option">Option.</param>
    void Register(PluginConfigOption option);

    /// <summary>
    /// Find option.
    /// </summary>
    /// <param name="pluginName">Plugin name.</param>
    /// <param name="name">Option name.</param>
    /// <returns>Option or null if not found.</returns>
    PluginConfigOption? Find(string pluginName, string name);

    /// <summary>
    /// Get registered options.
    /// </summary>
    /// <param name="pluginName">Plugin name. Returns options of all plugins if null or empty.</param>
    /// <returns>Options.</returns>
    IReadOnlyList<PluginConfigOption> GetOptions(string? pluginName = null);
}
