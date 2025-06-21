using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Execution;

namespace QueryCat.Cli.Commands.Options;

/// <summary>
/// Root of application: main application objects.
/// </summary>
internal sealed class ApplicationRoot : IDisposable
{
    /// <summary>
    /// Execution thread.
    /// </summary>
    public DefaultExecutionThread Thread { get; }

    /// <summary>
    /// Plugins manager.
    /// </summary>
    public IPluginsManager PluginsManager { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="thread">Execution thread.</param>
    /// <param name="pluginsManager">Plugins manager.</param>
    public ApplicationRoot(DefaultExecutionThread thread, IPluginsManager pluginsManager)
    {
        Thread = thread;
        PluginsManager = pluginsManager;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        (PluginsManager as IDisposable)?.Dispose();
        Thread.Dispose();
    }
}
