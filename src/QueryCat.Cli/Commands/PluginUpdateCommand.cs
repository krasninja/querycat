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
        this.SetHandler(async (context) =>
        {
            var applicationOptions = OptionsUtils.GetValueForOption(
                new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), context);
            var plugin = OptionsUtils.GetValueForOption(pluginArgument, context);

            applicationOptions.InitializeLogger();
            using var root = await applicationOptions.CreateApplicationRootAsync();
            await root.PluginsManager.UpdateAsync(plugin, context.GetCancellationToken());
        });
    }
}
#endif
