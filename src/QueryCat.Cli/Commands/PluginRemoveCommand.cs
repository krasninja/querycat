using System.CommandLine;
using QueryCat.Backend.Core.Utils;
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
        this.SetHandler((applicationOptions, plugin) =>
        {
            applicationOptions.InitializeLogger();
            AsyncUtils.RunSync(async () =>
            {
                using var root = applicationOptions.CreateApplicationRoot();
                await root.PluginsManager.RemoveAsync(plugin);
            });
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), pluginArgument);
    }
}
#endif
