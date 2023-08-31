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
    /// The token source to force current query cancel.
    /// </summary>
    CancellationTokenSource CancellationTokenSource { get; }

    /// <summary>
    /// Functions manager.
    /// </summary>
    FunctionsManager FunctionsManager { get; }

    /// <summary>
    /// Plugins manager.
    /// </summary>
    PluginsManager PluginsManager { get; }

    /// <summary>
    /// Config storage.
    /// </summary>
    IInputConfigStorage ConfigStorage { get; }

    /// <summary>
    /// Run text query.
    /// </summary>
    /// <param name="query">Query.</param>
    VariantValue Run(string query);
}
