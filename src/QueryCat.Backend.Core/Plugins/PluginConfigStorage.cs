using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Config storage scoped to the specific plugin. All keys are prefixed with the plugin name
/// (f.e. "url" is stored as "QueryCat.Plugins.Github:url"). For the declared options the value
/// is converted to the option type on set, and the default value is returned if not set.
/// </summary>
public sealed class PluginConfigStorage : IConfigStorage
{
    private readonly IConfigStorage _configStorage;
    private readonly IPluginConfigOptionsRegistry _optionsRegistry;

    /// <summary>
    /// Plugin name.
    /// </summary>
    public string PluginName { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="configStorage">Target config storage.</param>
    /// <param name="pluginName">Plugin name.</param>
    /// <param name="optionsRegistry">Plugins options registry.</param>
    public PluginConfigStorage(IConfigStorage configStorage, string pluginName, IPluginConfigOptionsRegistry optionsRegistry)
    {
        ArgumentException.ThrowIfNullOrEmpty(pluginName);

        _configStorage = configStorage;
        _optionsRegistry = optionsRegistry;
        PluginName = pluginName;
    }

    /// <inheritdoc />
    public ValueTask SetAsync(string key, VariantValue value, CancellationToken cancellationToken = default)
    {
        var option = _optionsRegistry.Find(PluginName, key);
        if (option != null && !value.IsNull)
        {
            if (!value.TryCast(option.Type, out var castedValue))
            {
                throw new PluginException(string.Format(Resources.Errors.ConfigOptionInvalidType,
                    key, PluginName, option.Type));
            }
            value = castedValue;
        }
        return _configStorage.SetAsync(PluginConfigOption.GetKey(PluginName, key), value, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasAsync(string key, CancellationToken cancellationToken = default)
        => !(await GetAsync(key, cancellationToken)).IsNull;

    /// <inheritdoc />
    public async ValueTask<VariantValue> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _configStorage.GetAsync(PluginConfigOption.GetKey(PluginName, key), cancellationToken);
        if (value.IsNull)
        {
            var option = _optionsRegistry.Find(PluginName, key);
            if (option != null)
            {
                return option.DefaultValue;
            }
        }
        return value;
    }

    /// <inheritdoc />
    public Task SaveAsync(CancellationToken cancellationToken = default) => _configStorage.SaveAsync(cancellationToken);

    /// <inheritdoc />
    public Task LoadAsync(CancellationToken cancellationToken = default) => _configStorage.LoadAsync(cancellationToken);
}
