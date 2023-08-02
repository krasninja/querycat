using QueryCat.Backend.Abstractions;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Query context oriented for plugins.
/// </summary>
public sealed class PluginQueryContext : QueryContext
{
    /// <inheritdoc />
    public override QueryContextQueryInfo QueryInfo { get; }

    public PluginQueryContext(QueryContextQueryInfo queryInfo, IInputConfigStorage inputConfigStorage)
    {
        QueryInfo = queryInfo;
        InputConfigStorage = inputConfigStorage;
    }
}
