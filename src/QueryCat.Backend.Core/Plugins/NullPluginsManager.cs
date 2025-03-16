namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Plugins manager with no implementation.
/// </summary>
public sealed class NullPluginsManager : IPluginsManager
{
    /// <summary>
    /// Instance of <see cref="NullPluginsManager" />.
    /// </summary>
    public static IPluginsManager Instance { get; } = new NullPluginsManager();

    /// <inheritdoc />
    public IPluginsLoader PluginsLoader { get; } = NullPluginsLoader.Instance;

    /// <inheritdoc />
    public Task<IEnumerable<PluginInfo>> ListAsync(bool localOnly = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Array.Empty<PluginInfo>().AsEnumerable());
    }

    /// <inheritdoc />
    public Task<int> InstallAsync(string name, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public Task UpdateAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
