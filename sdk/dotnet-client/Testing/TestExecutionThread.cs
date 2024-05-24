using System;
using System.Collections.Generic;
using System.Threading;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Client.Testing;

/// <summary>
/// Execution thread to be used for testing.
/// </summary>
public class TestExecutionThread : IExecutionThread
{
    /// <inheritdoc />
    public IFunctionsManager FunctionsManager => NullFunctionsManager.Instance;

    /// <inheritdoc />
    public IPluginsManager PluginsManager => NullPluginsManager.Instance;

    /// <inheritdoc />
    public IInputConfigStorage ConfigStorage { get; } = new TestInputConfigStorage();

    /// <inheritdoc />
    public IExecutionScope TopScope => NullExecutionScope.Instance;

    /// <inheritdoc />
    public event EventHandler<ResolveVariableEventArgs>? VariableResolving;

    /// <inheritdoc />
    public event EventHandler<ResolveVariableEventArgs>? VariableResolved;

    /// <inheritdoc />
    public IObjectSelector ObjectSelector => NullObjectSelector.Instance;

    /// <inheritdoc />
    public ExecutionStatistic Statistic => NullExecutionStatistic.Instance;

    /// <inheritdoc />
    public object? Tag => null;

    /// <inheritdoc />
    public VariantValue Run(string query, IDictionary<string, VariantValue>? parameters = null, CancellationToken cancellationToken = default)
    {
        return default;
    }

    /// <inheritdoc />
    public bool TryGetVariable(string name, out VariantValue value, IExecutionScope? scope = null)
    {
        value = default;
        return false;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
