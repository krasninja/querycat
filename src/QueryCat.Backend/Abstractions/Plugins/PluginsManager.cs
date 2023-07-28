namespace QueryCat.Backend.Abstractions.Plugins;

/// <summary>
/// List/install/remove/update plugins.
/// </summary>
public abstract class PluginsManager
{
    /// <summary>
    /// List all local and remote plugins.
    /// </summary>
    /// <param name="localOnly">List local plugins only.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of plugins info.</returns>
    public abstract Task<IEnumerable<PluginInfo>> ListAsync(
        bool localOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Install the plugin from remote repository.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of created files.</returns>
    public abstract Task<int> InstallAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the plugin. Remove all current versions and install the new one.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public abstract Task UpdateAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove the plugin.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public abstract Task RemoveAsync(string name, CancellationToken cancellationToken = default);
}
