using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Backend.ThriftPlugins;

/// <summary>
/// The exception occurs when plugin proxy is not found in its default location.
/// </summary>
[Serializable]
public sealed class ProxyNotFoundException : PluginException
{
    public ProxyNotFoundException(string pluginName)
        : base(string.Format(Resources.Errors.CannotFindPluginsProxy, pluginName))
    {
    }
}
