using System.CommandLine;
using QueryCat.Backend.Utils;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginUpdateCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginUpdateCommand() : base("update", "Update the plugin.")
    {
        var pluginArgument = new Argument<string>("plugin", "Plugin name.");

        this.AddArgument(pluginArgument);
        this.SetHandler((queryOptions, plugin) =>
        {
            queryOptions.InitializeLogger();
            AsyncUtils.RunSync(async () =>
            {
                using var executionThread = queryOptions.CreateExecutionThread();
                await executionThread.PluginsManager.UpdateAsync(plugin);
            });
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), pluginArgument);
    }
}
#endif
