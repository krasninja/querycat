using McMaster.Extensions.CommandLineUtils;

namespace QueryCat.Cli;

#if ENABLE_PLUGINS
[Command("update", Description = "Update the plugin.")]
public class PluginUpdateCommand : BasePluginCommand
{
    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        base.OnExecute(app, console);

        var runner = CreateRunner();
        runner.ExecutionThread.PluginsManager.UpdateAsync(Query, CancellationToken.None).GetAwaiter().GetResult();
        return 1;
    }
}
#endif
