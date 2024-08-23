using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Execution thread that does nothing.
/// </summary>
public sealed class NullExecutionThread : IExecutionThread
{
    /// <summary>
    /// Static instance of <see cref="NullExecutionThread" />.
    /// </summary>
    public static NullExecutionThread Instance { get; } = new();

    /// <inheritdoc />
    public IFunctionsManager FunctionsManager { get; } = NullFunctionsManager.Instance;

    /// <inheritdoc />
    public IPluginsManager PluginsManager { get; } = NullPluginsManager.Instance;

    /// <inheritdoc />
    public IInputConfigStorage ConfigStorage { get; } = NullInputConfigStorage.Instance;

    /// <inheritdoc />
    public IExecutionScope TopScope { get; } = NullExecutionScope.Instance;

#pragma warning disable CS0067
    /// <inheritdoc />
    public event EventHandler<ResolveVariableEventArgs>? VariableResolving;

    /// <inheritdoc />
    public event EventHandler<ResolveVariableEventArgs>? VariableResolved;
#pragma warning disable CS0067

    /// <inheritdoc />
    public IObjectSelector ObjectSelector { get; } = NullObjectSelector.Instance;

    /// <inheritdoc />
    public string CurrentQuery { get; } = string.Empty;

    /// <inheritdoc />
    public ExecutionStatistic Statistic { get; } = NullExecutionStatistic.Instance;

    /// <inheritdoc />
    public object? Tag { get; } = null;

    /// <inheritdoc />
    public VariantValue Run(string query, IDictionary<string, VariantValue>? parameters = null,
        CancellationToken cancellationToken = default) => VariantValue.Null;

    /// <inheritdoc />
    public bool TryGetVariable(string name, out VariantValue value, IExecutionScope? scope = null)
    {
        value = VariantValue.Null;
        return false;
    }

    /// <inheritdoc />
    public IEnumerable<CompletionItem> GetCompletions(string query, int position = -1, object? tag = null)
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
