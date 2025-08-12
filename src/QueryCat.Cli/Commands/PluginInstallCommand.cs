using System.CommandLine;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginInstallCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginInstallCommand() : base("install", Resources.Messages.PluginInstallCommand_Description)
    {
        var pluginArgument = new Argument<string>("plugin")
        {
            Description = Resources.Messages.PluginInstallCommand_NameDescription,
        };
        var overwriteOption = new Option<bool>("--overwrite", "-o")
        {
            Description = Resources.Messages.PluginInstallCommand_OverwriteDescription,
            DefaultValueFactory = _ => true,
        };

        this.Add(pluginArgument);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.Configuration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var plugin = parseResult.GetRequiredValue(pluginArgument);
            var overwrite = parseResult.GetValue(overwriteOption);

            applicationOptions.InitializeLogger();
            applicationOptions.InitializeAIAssistant();
            await using var root = await applicationOptions.CreateApplicationRootAsync();
            await root.PluginsManager.InstallAsync(plugin, overwrite, cancellationToken);
            await ApplicationOptions.InstallPluginsProxyAsync(
                askUser: false,
                skipIfExists: true,
                cancellationToken: cancellationToken);
        });
    }
}
#endif
