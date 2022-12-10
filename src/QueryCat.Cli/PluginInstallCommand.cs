using McMaster.Extensions.CommandLineUtils;

namespace QueryCat.Cli;

#if ENABLE_PLUGINS
[Command("install", Description = "Install the plugin.")]
public class PluginInstallCommand : BasePluginCommand
{
    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        base.OnExecute(app, console);

        var runner = CreateRunner();
        runner.ExecutionThread.PluginsManager.InstallAsync(Query, CancellationToken.None).GetAwaiter().GetResult();
        return 1;
    }
}
#endif
