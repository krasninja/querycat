using System.CommandLine;
using QueryCat.Backend.Core;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginListCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginListCommand() : base("list", Resources.Messages.PluginListCommand_Description)
    {
        var listAllArgument = new Option<bool>("--all")
        {
            Description = Resources.Messages.PluginListCommand_AllDescription
        };

        this.Add(listAllArgument);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.Configuration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var listAll = parseResult.GetValue(listAllArgument);

            applicationOptions.InitializeLogger();
            applicationOptions.InitializeAIAssistant();
            await using var root = await applicationOptions.CreateStdoutApplicationRootAsync(
                columnsSeparator: parseResult.GetValue(ColumnsSeparatorOption),
                outputStyle: parseResult.GetValue(OutputStyleOption)
            );
            var query = "SELECT * FROM _plugins() WHERE 1=1";
            if (!listAll)
            {
                query += $@" AND (platform = _platform() OR platform = '{Application.PlatformMulti}');";
            }
            var result = await root.Thread.RunAsync(query, cancellationToken: cancellationToken);
            await WriteAsync(root.Thread, result, root.RowsOutput, cancellationToken);
        });
    }
}
#endif
