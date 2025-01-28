using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Execution;

namespace QueryCat.Cli.Commands.Options;

/// <summary>
/// Root of application: main application objects.
/// </summary>
internal sealed class ApplicationRoot : IDisposable
{
    public DefaultExecutionThread Thread { get; }

    public IPluginsManager PluginsManager { get; }

    public CancellationTokenSource CancellationTokenSource { get; } = new();

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
        CancellationTokenSource.Dispose();
    }
}
