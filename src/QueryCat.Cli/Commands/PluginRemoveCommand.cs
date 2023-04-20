using System.CommandLine;
using QueryCat.Backend.Utils;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginRemoveCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginRemoveCommand() : base("remove", "Remove the plugin.")
    {
        var pluginArgument = new Argument<string>("plugin", "Plugin name.");

        this.AddArgument(pluginArgument);
        this.SetHandler((queryOptions, plugin) =>
        {
            queryOptions.InitializeLogger();
            AsyncUtils.RunSync(async () =>
            {
                using var executionThread = queryOptions.CreateExecutionThread();
                await executionThread.PluginsManager.RemoveAsync(plugin);
            });
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), pluginArgument);
    }
}
#endif
