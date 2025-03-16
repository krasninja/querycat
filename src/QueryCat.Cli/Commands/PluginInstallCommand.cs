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
        var overwriteOption = new Option<bool>(
            ["--overwrite", "-o"],
            getDefaultValue: () => true,
            "Overwrite the plugin if it already exists.");

        this.AddArgument(pluginArgument);
        this.SetHandler(async (context) =>
        {
            var applicationOptions = OptionsUtils.GetValueForOption(
                new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), context);
            var plugin = OptionsUtils.GetValueForOption(pluginArgument, context);
            var overwrite = OptionsUtils.GetValueForOption(overwriteOption, context);

            applicationOptions.InitializeLogger();
            using var root = await applicationOptions.CreateApplicationRootAsync();
            await root.PluginsManager.InstallAsync(plugin, overwrite, context.GetCancellationToken());
            await ApplicationOptions.InstallPluginsProxyAsync(
                askUser: false,
                skipIfExists: true,
                cancellationToken: context.GetCancellationToken());
        });
    }
}
#endif
