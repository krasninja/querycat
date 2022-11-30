using McMaster.Extensions.CommandLineUtils;

namespace QueryCat.Cli;

#if ENABLE_PLUGINS
[Command("plugin", Description = "Command to work with plugins.")]
[Subcommand(
    typeof(PluginListCommand),
    typeof(PluginInstallCommand),
    typeof(PluginRemoveCommand),
    typeof(PluginUpdateCommand))]
public class PluginCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        app.ShowHelp();
        return 1;
    }
}
#endif
