namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Plugins loading strategy.
/// </summary>
public interface IPluginsLoader
{
    /// <summary>
    /// Load plugins.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of loaded plugins.</returns>
    Task<int> LoadAsync(CancellationToken cancellationToken = default);
}
