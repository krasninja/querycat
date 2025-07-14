using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Execution;

namespace QueryCat.Cli.Commands.Options;

/// <summary>
/// Root of application: main application objects.
/// </summary>
internal sealed class ApplicationRoot : IDisposable, IAsyncDisposable
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
    /// Application level rows output.
    /// </summary>
    public IRowsOutput RowsOutput { get; set; } = NullRowsOutput.Instance;

    private bool _isDisposed;

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
        if (_isDisposed)
        {
            return;
        }

        (PluginsManager as IDisposable)?.Dispose();
        Thread.Dispose();
        _isDisposed = true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        (PluginsManager as IDisposable)?.Dispose();
        await Thread.DisposeAsync();
        _isDisposed = true;
    }
}
