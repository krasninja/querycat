using QueryCat.Backend.Core.Data;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Query context oriented for plugins.
/// </summary>
public sealed class PluginQueryContext : QueryContext
{
    public PluginQueryContext(QueryContextQueryInfo queryInfo, IConfigStorage configStorage) : base(queryInfo)
    {
        ConfigStorage = configStorage;
    }
}
