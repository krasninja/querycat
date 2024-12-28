using System.CommandLine;
using QueryCat.Backend.Core.Utils;
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
        this.SetHandler(async (applicationOptions, plugin) =>
        {
            applicationOptions.InitializeLogger();
            using var root = applicationOptions.CreateApplicationRoot();
            await root.PluginsManager.InstallAsync(plugin);
            await ApplicationOptions.InstallPluginsProxyAsync(askUser: false);
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), pluginArgument);
    }
}
#endif
