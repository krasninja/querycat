using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Execution;

namespace QueryCat.Cli.Commands.Options;

/// <summary>
/// Root of application: main application objects.
/// </summary>
internal sealed class ApplicationRoot : IDisposable
{
    public ExecutionThread Thread { get; }

    public PluginsManager PluginsManager { get; }

    public PluginsLoader PluginsLoader { get; }

    public ApplicationRoot(ExecutionThread thread, PluginsManager pluginsManager, PluginsLoader pluginsLoader)
    {
        Thread = thread;
        PluginsManager = pluginsManager;
        PluginsLoader = pluginsLoader;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        (PluginsManager as IDisposable)?.Dispose();
        (PluginsLoader as IDisposable)?.Dispose();
        Thread.Dispose();
    }
}
