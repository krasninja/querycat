using System.Collections.Generic;
using System.Threading;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Plugins.Sdk;
using PluginsManager = QueryCat.Plugins.Sdk.PluginsManager;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Plugins.Client;

/// <summary>
/// The implementation of execution thread that uses remote input config storage.
/// </summary>
public sealed class PluginExecutionThread : IExecutionThread
{
    /// <inheritdoc />
    public IFunctionsManager FunctionsManager { get; }

    /// <inheritdoc />
    public IPluginsManager PluginsManager { get; }

    /// <inheritdoc />
    public IInputConfigStorage ConfigStorage { get; }

    /// <inheritdoc />
    public IExecutionScope TopScope { get; } = NullExecutionScope.Instance;

    public PluginExecutionThread(PluginsManager.Client client)
    {
        PluginsManager = NullPluginsManager.Instance;
        FunctionsManager = new PluginFunctionsManager();
        ConfigStorage = new ThriftInputConfigStorage(client);
    }

    /// <inheritdoc />
    public VariantValue Run(string query, IDictionary<string, VariantValue>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        throw new QueryCatPluginException(ErrorType.GENERIC, "Query run is not supported within plugins.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
