namespace QueryCat.Backend.Core.Plugins;

public sealed class NullPluginsManager : IPluginsManager
{
    public static IPluginsManager Instance { get; } = new NullPluginsManager();

    /// <inheritdoc />
    public Task<IEnumerable<PluginInfo>> ListAsync(bool localOnly = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Array.Empty<PluginInfo>().AsEnumerable());
    }

    /// <inheritdoc />
    public Task<int> InstallAsync(string name, CancellationToken cancellationToken = default)
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
