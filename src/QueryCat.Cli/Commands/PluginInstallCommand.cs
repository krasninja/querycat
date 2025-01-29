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
        this.SetHandler(async (context) =>
        {
            var applicationOptions = OptionsUtils.GetValueForOption(
                new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), context);
            var plugin = OptionsUtils.GetValueForOption(pluginArgument, context);

            applicationOptions.InitializeLogger();
            using var root = await applicationOptions.CreateApplicationRootAsync();
            await root.PluginsManager.InstallAsync(plugin, cancellationToken: context.GetCancellationToken());
            await ApplicationOptions.InstallPluginsProxyAsync(askUser: false, cancellationToken: context.GetCancellationToken());
        });
    }
}
#endif
