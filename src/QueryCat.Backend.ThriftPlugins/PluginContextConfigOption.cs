using System.Text.Json.Serialization;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.ThriftPlugins;

/// <summary>
/// Plugin configuration option. It is stored in plugins functions cache.
/// </summary>
internal sealed record PluginContextConfigOption(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] DataType Type,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("default")] string? DefaultValue,
    [property: JsonPropertyName("default_type")] DataType DefaultValueType,
    [property: JsonPropertyName("plugin_name")] string PluginName
)
{
    public static PluginContextConfigOption Create(PluginConfigOption option)
        => new(
            option.Name,
            option.Type,
            option.Description,
            DataTypeUtils.SerializeVariantValue(option.DefaultValue),
            option.DefaultValue.Type,
            option.PluginName);

    public PluginConfigOption ToPluginConfigOption()
    {
        var defaultValue = VariantValue.Null;
        if (DefaultValue != null)
        {
            defaultValue = DataTypeUtils.DeserializeVariantValue(DefaultValue);
        }
        return new PluginConfigOption(Name, Type, Description, defaultValue)
        {
            PluginName = PluginName,
        };
    }
}
