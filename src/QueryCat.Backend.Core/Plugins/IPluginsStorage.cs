namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// List and download selected plugin.
/// </summary>
public interface IPluginsStorage
{
    /// <summary>
    /// List all plugins.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available plugins.</returns>
    Task<IReadOnlyList<PluginInfo>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Download the selected plugin.
    /// </summary>
    /// <param name="uri">Plugin URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The plugin content stream.</returns>
    Task<Stream> DownloadAsync(string uri, CancellationToken cancellationToken = default);
}
