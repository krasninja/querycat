namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Plugins loading strategy.
/// </summary>
public interface IPluginsLoader
{
    /// <summary>
    /// Configuration options declared by the loaded plugins.
    /// </summary>
    IPluginConfigOptionsRegistry ConfigOptions { get; }

    /// <summary>
    /// Load plugins.
    /// </summary>
    /// <param name="options">Loading options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of loaded plugins.</returns>
    Task<int> LoadAsync(PluginsLoadingOptions options, CancellationToken cancellationToken = default);
}
