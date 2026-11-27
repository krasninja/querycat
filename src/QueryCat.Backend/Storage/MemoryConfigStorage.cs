using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// In memory storage for rows input.
/// </summary>
public class MemoryConfigStorage : IConfigStorage
{
    private readonly string _pluginPrefix;

    protected Dictionary<string, VariantValue> Map { get; } = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="pluginPrefix">Plugin prefix.</param>
    public MemoryConfigStorage(string? pluginPrefix = null)
    {
        _pluginPrefix = pluginPrefix ?? string.Empty;
    }

    /// <inheritdoc />
    public virtual ValueTask SetAsync(string key, VariantValue value, CancellationToken cancellationToken = default)
    {
        key = PluginConfigOption.GetKey(_pluginPrefix, key);
        if (value.IsNull)
        {
            Map.Remove(key);
        }
        else
        {
            Map[key] = value;
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public virtual ValueTask<bool> HasAsync(string key, CancellationToken cancellationToken = default)
    {
        key = PluginConfigOption.GetKey(_pluginPrefix, key);
        return ValueTask.FromResult(Map.ContainsKey(key));
    }

    /// <inheritdoc />
    public virtual ValueTask<VariantValue> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        key = PluginConfigOption.GetKey(_pluginPrefix, key);
        if (Map.TryGetValue(key, out var value))
        {
            return ValueTask.FromResult(value);
        }
        return ValueTask.FromResult(VariantValue.Null);
    }

    /// <inheritdoc />
    public virtual Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
