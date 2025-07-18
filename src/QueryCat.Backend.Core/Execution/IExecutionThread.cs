using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// The execution thread allows to run string commands.
/// </summary>
/// <remarks>
/// It is not related to operation-system thread.
/// </remarks>
public interface IExecutionThread : IDisposable
{
    /// <summary>
    /// Functions manager.
    /// </summary>
    IFunctionsManager FunctionsManager { get; }

    /// <summary>
    /// Plugins manager.
    /// </summary>
    IPluginsManager PluginsManager { get; }

    /// <summary>
    /// Config storage.
    /// </summary>
    IConfigStorage ConfigStorage { get; }

    /// <summary>
    /// Top execution scope.
    /// </summary>
    IExecutionScope TopScope { get; }

    /// <summary>
    /// Function arguments stack.
    /// </summary>
    IExecutionStack Stack { get; }

    /// <summary>
    /// Selector to resolve object expressions.
    /// </summary>
    IObjectSelector ObjectSelector { get; }

    /// <summary>
    /// Currently executing query.
    /// </summary>
    string CurrentQuery { get; }

    /// <summary>
    /// Execution statistic.
    /// </summary>
    ExecutionStatistic Statistic { get; }

    /// <summary>
    /// Store custom execution thread information.
    /// </summary>
    object? Tag { get; }

    /// <summary>
    /// Run text query.
    /// </summary>
    /// <param name="query">Query.</param>
    /// <param name="parameters">Query parameters. They will be used in a separate scope.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the request execution.</param>
    Task<VariantValue> RunAsync(
        string query,
        IDictionary<string, VariantValue>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get completions for the incomplete query.
    /// </summary>
    /// <param name="text">Query text.</param>
    /// <param name="position">Caret position.</param>
    /// <param name="tag">Custom User data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completion result.</returns>
    IAsyncEnumerable<CompletionResult> GetCompletionsAsync(string text, int position = -1, object? tag = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create the new variables and execution scope based on top of the current one.
    /// </summary>
    /// <returns>Instance of <see cref="IExecutionScope" />.</returns>
    IExecutionScope PushScope();

    /// <summary>
    /// Pop the current execution scope for stack and return it.
    /// </summary>
    /// <returns>Instance of <see cref="IExecutionScope" /> or null if it is the top scope.</returns>
    IExecutionScope? PopScope();
}

/// <summary>
/// Execution thread with options.
/// </summary>
/// <typeparam name="TOptions">Options type.</typeparam>
public interface IExecutionThread<out TOptions> : IExecutionThread
{
    /// <summary>
    /// Application options.
    /// </summary>
    TOptions Options { get; }
}
