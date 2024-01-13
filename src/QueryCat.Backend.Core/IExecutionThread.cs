using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core;

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
