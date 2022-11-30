using McMaster.Extensions.CommandLineUtils;

namespace QueryCat.Cli;

#if ENABLE_PLUGINS
[Command("remove", Description = "Remove the plugin.")]
public class PluginRemoveCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        var runner = CreateRunner();
        runner.ExecutionThread.PluginsManager.RemoveAsync(Query, CancellationToken.None).GetAwaiter().GetResult();
        return 1;
    }
}
#endif
