using QueryCat.Backend.Execution;
using PluginsManager = QueryCat.Plugins.Sdk.PluginsManager;

namespace QueryCat.Plugins.Client;

/// <summary>
/// The implementation of execution thread that uses remote input config storage.
/// </summary>
public class PluginExecutionThread : ExecutionThread
{
    public PluginExecutionThread(PluginsManager.Client client)
    {
        ConfigStorage = new ThriftInputConfigStorage(client);
    }
}
