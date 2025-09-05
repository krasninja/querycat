using System.CommandLine;
using QueryCat.Backend.Core;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginListCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginListCommand() : base("list", Resources.Messages.PluginListCommand_Description)
    {
        var listAllOption = new Option<bool>("--all")
        {
            Description = Resources.Messages.PluginListCommand_AllDescription
        };

        this.Add(listAllOption);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var listAll = parseResult.GetValue(listAllOption);

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
