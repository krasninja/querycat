using System.CommandLine;
using QueryCat.Backend.Utils;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginInstallCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginInstallCommand() : base("install", "Install the plugin.")
    {
        var pluginArgument = new Argument<string>("plugin", "Plugin name.");

        this.AddArgument(pluginArgument);
        this.SetHandler((applicationOptions, plugin) =>
        {
            applicationOptions.InitializeLogger();
            AsyncUtils.RunSync(async ct =>
            {
                using var root = applicationOptions.CreateApplicationRoot();
                await root.PluginsManager.InstallAsync(plugin, ct);
            });
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), pluginArgument);
    }
}
#endif
