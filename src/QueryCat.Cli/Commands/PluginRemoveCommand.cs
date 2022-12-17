using System.CommandLine;
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
            var executionThread = queryOptions.CreateExecutionThread();
            executionThread.PluginsManager.RemoveAsync(plugin, CancellationToken.None).GetAwaiter().GetResult();
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), pluginArgument);
    }
}
#endif
