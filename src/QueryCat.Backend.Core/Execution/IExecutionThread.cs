using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// The execution thread allows to run string commands.
/// </summary>
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
    IInputConfigStorage ConfigStorage { get; }

    /// <summary>
    /// Top execution scope.
    /// </summary>
    IExecutionScope TopScope { get; }

    /// <summary>
    /// Function arguments stack.
    /// </summary>
    IExecutionStack Stack { get; }

    /// <summary>
    /// The event is fired before variable resolving.
    /// </summary>
    event EventHandler<ResolveVariableEventArgs>? VariableResolving;

    /// <summary>
    /// The event is fired after variable resolve.
    /// </summary>
    event EventHandler<ResolveVariableEventArgs>? VariableResolved;

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
    VariantValue Run(
        string query,
        IDictionary<string, VariantValue>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Try to get variable value from top scope to the root recursively.
    /// </summary>
    /// <param name="name">Variable name.</param>
    /// <param name="value">Variable value.</param>
    /// <param name="scope">Scope instance. Top scope is used by default.</param>
    /// <returns>True if variable with the specified name is found, false otherwise.</returns>
    bool TryGetVariable(string name, out VariantValue value, IExecutionScope? scope = null);

    /// <summary>
    /// Get completions for the incomplete query.
    /// </summary>
    /// <param name="text">Query text.</param>
    /// <param name="position">Caret position.</param>
    /// <param name="tag">Custom User data.</param>
    /// <returns>Completion result.</returns>
    IEnumerable<CompletionResult> GetCompletions(string text, int position = -1, object? tag = null);

    /// <summary>
    /// Create the new variables scope based on top of the current.
    /// </summary>
    /// <returns>Instance of <see cref="IExecutionScope" />.</returns>
    IExecutionScope PushScope();

    /// <summary>
    /// Pop the current execution scope for stack and return it.
    /// </summary>
    /// <returns>Instance of <see cref="IExecutionScope" />.</returns>
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
