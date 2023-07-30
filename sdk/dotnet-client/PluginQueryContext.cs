using QueryCat.Backend.Storage;

namespace QueryCat.Plugins.Client;

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
