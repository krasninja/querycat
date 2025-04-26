using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

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

    /// <inheritdoc />
    public IExecutionStack Stack { get; } = NullExecutionStack.Instance;

    /// <inheritdoc />
    public IObjectSelector ObjectSelector { get; } = NullObjectSelector.Instance;

    /// <inheritdoc />
    public string CurrentQuery { get; } = string.Empty;

    /// <inheritdoc />
    public ExecutionStatistic Statistic { get; } = NullExecutionStatistic.Instance;

    /// <inheritdoc />
    public object? Tag { get; } = null;

    /// <inheritdoc />
    public Task<VariantValue> RunAsync(string query, IDictionary<string, VariantValue>? parameters = null,
        CancellationToken cancellationToken = default) => Task.FromResult(VariantValue.Null);

    /// <inheritdoc />
    public IAsyncEnumerable<CompletionResult> GetCompletionsAsync(string query, int position = -1, object? tag = null,
        CancellationToken cancellationToken = default)
    {
        return AsyncUtils.Empty<CompletionResult>();
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
