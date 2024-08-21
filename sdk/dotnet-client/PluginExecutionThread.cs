using System;
using System.Collections.Generic;
using System.Threading;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
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
    public IExecutionScope TopScope => NullExecutionScope.Instance;

#pragma warning disable CS0067
    /// <inheritdoc />
    public event EventHandler<ResolveVariableEventArgs>? VariableResolving;

    /// <inheritdoc />
    public event EventHandler<ResolveVariableEventArgs>? VariableResolved;
#pragma warning disable CS0067

    /// <inheritdoc />
    public IObjectSelector ObjectSelector => NullObjectSelector.Instance;

    /// <inheritdoc />
    public string CurrentQuery => string.Empty;

    /// <inheritdoc />
    public ExecutionStatistic Statistic => NullExecutionStatistic.Instance;

    /// <inheritdoc />
    public object? Tag => null;

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
        throw new QueryCatPluginException(ErrorType.GENERIC, Resources.Errors.NotSupported_QueryRun);
    }

    /// <inheritdoc />
    public bool TryGetVariable(string name, out VariantValue value, IExecutionScope? scope = null)
    {
        value = VariantValue.Null;
        return false;
    }

    /// <inheritdoc />
    public IEnumerable<CompletionItem> GetCompletions(string query, int position = -1)
    {
        yield break;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
