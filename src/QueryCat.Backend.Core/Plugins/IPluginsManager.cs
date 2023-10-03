namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// List/install/remove/update plugins.
/// </summary>
public interface IPluginsManager
{
    /// <summary>
    /// List all local and remote plugins.
    /// </summary>
    /// <param name="localOnly">List local plugins only.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of plugins info.</returns>
    Task<IEnumerable<PluginInfo>> ListAsync(
        bool localOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Install the plugin from remote repository.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of created files.</returns>
    Task<int> InstallAsync(string name, CancellationToken cancellationToken = default);

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
}
