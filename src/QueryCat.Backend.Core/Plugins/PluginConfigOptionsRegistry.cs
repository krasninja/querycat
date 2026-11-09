using System.Collections.Concurrent;

namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Thread-safe in-memory registry of plugins configuration options.
/// </summary>
public sealed class PluginConfigOptionsRegistry : IPluginConfigOptionsRegistry
{
    private readonly ConcurrentDictionary<string, PluginConfigOption> _options = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void Register(PluginConfigOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        _options[option.Key] = option;
    }

    /// <inheritdoc />
    public PluginConfigOption? Find(string pluginName, string name)
        => _options.GetValueOrDefault(PluginConfigOption.GetKey(pluginName, name));

    /// <inheritdoc />
    public IReadOnlyList<PluginConfigOption> GetOptions(string? pluginName = null)
        => _options.Values
            .Where(o => string.IsNullOrEmpty(pluginName)
                        || o.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(o => o.PluginName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
