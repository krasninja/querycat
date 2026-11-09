using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Configuration option declared by a plugin. The option value is stored in the host config storage
/// under the plugin specific key (see <see cref="GetKey(string, string)" />).
/// </summary>
public sealed class PluginConfigOption
{
    /// <summary>
    /// Separator between plugin name and option name in the config storage key.
    /// </summary>
    public const char KeySeparator = ':';

    /// <summary>
    /// Option name (f.e. "url").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Option value type. The value is converted to this type on set.
    /// Use <see cref="DataType.Dynamic" /> to skip the conversion.
    /// </summary>
    public DataType Type { get; }

    /// <summary>
    /// Option description. Optional.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Default value. It is returned if the option value is not set.
    /// </summary>
    public VariantValue DefaultValue { get; }

    /// <summary>
    /// The plugin name the option belongs to. It is set by the plugins loader.
    /// </summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// The config storage key.
    /// </summary>
    public string Key => GetKey(PluginName, Name);

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Option name.</param>
    /// <param name="type">Option value type.</param>
    /// <param name="description">Option description.</param>
    /// <param name="defaultValue">Default value.</param>
    public PluginConfigOption(string name, DataType type, string? description = null, VariantValue defaultValue = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
        Type = type;
        Description = description ?? string.Empty;
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Get the config storage key for the plugin option.
    /// </summary>
    /// <param name="pluginName">Plugin name.</param>
    /// <param name="name">Option name.</param>
    /// <returns>Storage key.</returns>
    public static string GetKey(string pluginName, string name)
        => string.IsNullOrEmpty(pluginName) ? name : pluginName + KeySeparator + name;

    /// <inheritdoc />
    public override string ToString() => Key;
}
