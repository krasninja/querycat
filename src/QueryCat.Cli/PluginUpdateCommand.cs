using McMaster.Extensions.CommandLineUtils;

namespace QueryCat.Cli;

[Command("update", Description = "Update the plugin.")]
public class PluginUpdateCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        var runner = CreateRunner();
        runner.ExecutionThread.PluginsManager.UpdateAsync(Query, CancellationToken.None).GetAwaiter().GetResult();
        return 1;
    }
}
