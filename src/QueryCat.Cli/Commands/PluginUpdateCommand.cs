using System.CommandLine;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginUpdateCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginUpdateCommand() : base("update", Resources.Messages.PluginUpdateCommand_Description)
    {
        var pluginArgument = new Argument<string>("plugin")
        {
            Description = Resources.Messages.PluginUpdateCommand_NameDescription,
        };

        this.Add(pluginArgument);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.Configuration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var plugin = parseResult.GetRequiredValue(pluginArgument);

            applicationOptions.InitializeLogger();
            applicationOptions.InitializeAIAssistant();
            await using var root = await applicationOptions.CreateApplicationRootAsync();
            await root.PluginsManager.UpdateAsync(plugin, cancellationToken);
        });
    }
}
#endif
