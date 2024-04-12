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
    /// Selector to resolve object expressions.
    /// </summary>
    IObjectSelector ObjectSelector { get; }

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
    /// <param name="parameters">Query parameters.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the request execution.</param>
    VariantValue Run(
        string query,
        IDictionary<string, VariantValue>? parameters = null,
        CancellationToken cancellationToken = default);
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
