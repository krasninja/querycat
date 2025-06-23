using System.CommandLine;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginRemoveCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginRemoveCommand() : base("remove", Resources.Messages.PluginRemoveCommand_Description)
    {
        var pluginArgument = new Argument<string>("plugin")
        {
            Description = Resources.Messages.PluginRemoveCommand_NameDescription,
        };

        this.Add(pluginArgument);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.Configuration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var plugin = parseResult.GetRequiredValue(pluginArgument);

            applicationOptions.InitializeLogger();
            await using var root = await applicationOptions.CreateApplicationRootAsync();
            await root.PluginsManager.RemoveAsync(plugin, cancellationToken);
        });
    }
}
#endif
