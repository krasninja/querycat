using QueryCat.Backend.Abstractions.Plugins;

namespace QueryCat.Backend.Execution;

public sealed class NullPluginsManager : PluginsManager
{
    public static PluginsManager Instance { get; } = new NullPluginsManager();

    /// <inheritdoc />
    public override Task<IEnumerable<PluginInfo>> ListAsync(bool localOnly = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Array.Empty<PluginInfo>().AsEnumerable());
    }

    /// <inheritdoc />
    public override Task<int> InstallAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public override Task UpdateAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task RemoveAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
