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
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Plugins.Client;

/// <summary>
/// The implementation of execution thread that uses remote config storage.
/// </summary>
/// <remarks>
/// The implementation has the following limitations:
/// - The separate execution stack. When plugin function is called - we copy input args.
/// - No object selector.
/// - No variables resolve events.
/// - Local stack only.
/// - No current statistic.
/// </remarks>
public sealed class ThriftPluginExecutionThread : IExecutionThread
{
    private readonly ThriftPluginClient _client;

    /// <inheritdoc />
    public IFunctionsManager FunctionsManager { get; }

    /// <inheritdoc />
    public IPluginsManager PluginsManager { get; }

    /// <inheritdoc />
    public IConfigStorage ConfigStorage { get; }

    /// <inheritdoc />
    public IExecutionScope TopScope
    {
        get
        {
            var scope = AsyncUtils.RunSync(ct => _client.ThriftClient.PeekTopScopeAsync(_client.Token, ct));
            if (scope == null)
            {
                throw new InvalidOperationException("Cannot get scope.");
            }
            return new ThriftPluginExecutionScope(_client, scope.Id, scope.ParentId);
        }
    }

    /// <inheritdoc />
    public IExecutionStack Stack { get; }

    /// <inheritdoc />
    public IObjectSelector ObjectSelector => NullObjectSelector.Instance;

    /// <inheritdoc />
    public string CurrentQuery { get; private set; } = string.Empty;

    /// <inheritdoc />
    public ExecutionStatistic Statistic => NullExecutionStatistic.Instance;

    /// <inheritdoc />
    public object? Tag => null;

    public ThriftPluginExecutionThread(ThriftPluginClient client)
    {
        _client = client;
        PluginsManager = NullPluginsManager.Instance;
        FunctionsManager = new PluginFunctionsManager();
        ConfigStorage = new ThriftConfigStorage(_client);
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
            var result = await _client.ThriftClient.RunQueryAsync(
                _client.Token,
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
    public async IAsyncEnumerable<CompletionResult> GetCompletionsAsync(string query, int position = -1, object? tag = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var completions = await _client.ThriftClient.GetCompletionsAsync(_client.Token, query, position, cancellationToken);
        foreach (var completionResult in completions)
        {
            yield return SdkConvert.Convert(completionResult);
        }
    }

    /// <inheritdoc />
    public IExecutionScope PushScope()
    {
        var scope = AsyncUtils.RunSync(ct => _client.ThriftClient.PushScopeAsync(_client.Token, ct));
        if (scope == null)
        {
            throw new InvalidOperationException("Cannot create scope.");
        }
        return new ThriftPluginExecutionScope(_client, scope.Id, scope.ParentId);
    }

    /// <inheritdoc />
    public IExecutionScope? PopScope()
    {
        var scope = AsyncUtils.RunSync(ct => _client.ThriftClient.PopScopeAsync(_client.Token, ct));
        if (scope == null)
        {
            return null;
        }
        return new ThriftPluginExecutionScope(_client, scope.Id, scope.ParentId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
