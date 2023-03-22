using System.CommandLine;
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
            using var executionThread = queryOptions.CreateExecutionThread();
            executionThread.PluginsManager.UpdateAsync(plugin, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter().GetResult();
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), pluginArgument);
    }
}
#endif
