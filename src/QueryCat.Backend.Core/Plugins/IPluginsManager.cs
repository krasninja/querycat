namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// List/install/remove/update plugins.
/// </summary>
public interface IPluginsManager
{
    /// <summary>
    /// Implementation of plugins loading.
    /// </summary>
    IPluginsLoader PluginsLoader { get; }

    /// <summary>
    /// List all local and remote plugins.
    /// </summary>
    /// <param name="options">List options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of plugins info.</returns>
    Task<IEnumerable<PluginInfo>> ListAsync(
        PluginListOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Install the plugin from remote repository. If the name is a web URL (HTTP or HTTPS),
    /// the plugin file is downloaded directly from that URL.
    /// </summary>
    /// <param name="name">Plugin name or URL of the plugin file.</param>
    /// <param name="overwrite">Force install if plugin already exist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of created files.</returns>
    Task<int> InstallAsync(string name, bool overwrite = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the plugin. Remove all current versions and install the new one.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove the plugin.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task RemoveAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get configuration options declared by the loaded plugin.
    /// </summary>
    /// <param name="name">Plugin name. Returns options of all loaded plugins if empty.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration options.</returns>
    Task<IReadOnlyCollection<PluginConfigOption>> GetOptionsAsync(string name, CancellationToken cancellationToken = default);
}
