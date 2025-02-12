using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Utils;
using PluginsManager = QueryCat.Plugins.Sdk.PluginsManager;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Plugins.Client;

/// <summary>
/// The implementation of execution thread that uses remote input config storage.
/// </summary>
/// <remarks>
/// The implementation has the following limitations:
/// - The separate execution stack. When plugin function is called - we copy input args.
/// - No object selector.
/// - No execution scopes.
/// - No variables resolve events.
/// - Local stack only.
/// - No completions.
/// - No current statistic.
/// </remarks>
public sealed class ThriftPluginExecutionThread : IExecutionThread
{
    private readonly PluginsManager.Client _client;

    /// <inheritdoc />
    public IFunctionsManager FunctionsManager { get; }

    /// <inheritdoc />
    public IPluginsManager PluginsManager { get; }

    /// <inheritdoc />
    public IInputConfigStorage ConfigStorage { get; }

    /// <inheritdoc />
    public IExecutionScope TopScope { get; }

    /// <inheritdoc />
    public IExecutionStack Stack { get; }

#pragma warning disable CS0067
    /// <inheritdoc />
    public event EventHandler<ResolveVariableEventArgs>? VariableResolving;

    /// <inheritdoc />
    public event EventHandler<ResolveVariableEventArgs>? VariableResolved;
#pragma warning disable CS0067

    /// <inheritdoc />
    public IObjectSelector ObjectSelector => NullObjectSelector.Instance;

    /// <inheritdoc />
    public string CurrentQuery { get; private set; } = string.Empty;

    /// <inheritdoc />
    public ExecutionStatistic Statistic => NullExecutionStatistic.Instance;

    /// <inheritdoc />
    public object? Tag => null;

    public ThriftPluginExecutionThread(PluginsManager.Client client)
    {
        _client = client;
        PluginsManager = NullPluginsManager.Instance;
        FunctionsManager = new PluginFunctionsManager();
        TopScope = new ThriftPluginExecutionScope(_client);
        ConfigStorage = new ThriftInputConfigStorage(_client);
        Stack = new ListExecutionStack();
    }

    /// <inheritdoc />
    public async Task<VariantValue> RunAsync(
        string query,
        IDictionary<string, VariantValue>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CurrentQuery = query;
            var result = await _client.RunQueryAsync(
                query,
                parameters?.ToDictionary(k => k.Key, v => SdkConvert.Convert(v.Value)),
                cancellationToken);
            return SdkConvert.Convert(result);
        }
        finally
        {
            CurrentQuery = string.Empty;
        }
    }

    /// <inheritdoc />
    public bool TryGetVariable(string name, out VariantValue value, IExecutionScope? scope = null)
    {
        value = TopScope.Variables[name];
        return !value.IsNull;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CompletionResult> GetCompletionsAsync(string query, int position = -1, object? tag = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield break;
    }

    /// <inheritdoc />
    public IExecutionScope PushScope() => NullExecutionScope.Instance;

    /// <inheritdoc />
    public IExecutionScope? PopScope() => null;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
