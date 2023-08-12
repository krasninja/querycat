using System.Threading;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Abstractions.Plugins;
using QueryCat.Plugins.Sdk;
using PluginsManager = QueryCat.Plugins.Sdk.PluginsManager;
using VariantValue = QueryCat.Backend.Types.VariantValue;

namespace QueryCat.Plugins.Client;

/// <summary>
/// The implementation of execution thread that uses remote input config storage.
/// </summary>
public sealed class PluginExecutionThread : IExecutionThread
{
    /// <inheritdoc />
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    /// <inheritdoc />
    public FunctionsManager FunctionsManager { get; }

    /// <inheritdoc />
    public Backend.Abstractions.Plugins.PluginsManager PluginsManager { get; }

    /// <inheritdoc />
    public IInputConfigStorage ConfigStorage { get; }

    public PluginExecutionThread(PluginsManager.Client client)
    {
        PluginsManager = NullPluginsManager.Instance;
        FunctionsManager = new PluginFunctionsManager();
        ConfigStorage = new ThriftInputConfigStorage(client);
    }

    /// <inheritdoc />
    public VariantValue Run(string query)
    {
        throw new QueryCatPluginException(ErrorType.GENERIC, "Query run is not supported within plugins.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
