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
    /// Try get variable value from top scope to the root recursively.
    /// </summary>
    /// <param name="name">Variable name.</param>
    /// <param name="value">Variable value.</param>
    /// <param name="scope">Scope instance.</param>
    /// <returns>True if variable with the specified name is found, false otherwise.</returns>
    bool TryGetVariable(string name, out VariantValue value, IExecutionScope? scope = null);

    /// <summary>
    /// Get completions for the incomplete query.
    /// </summary>
    /// <param name="query">Query text.</param>
    /// <param name="position">Cursor position within the query. By default, the end of the query.</param>
    /// <returns>Completion items.</returns>
    IEnumerable<CompletionItem> GetCompletions(string query, int position = -1);
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
