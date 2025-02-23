using System.CommandLine;
using QueryCat.Backend.Core;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginListCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginListCommand() : base("list", "List available plugins for the current platform.")
    {
        var listAllArgument = new Option<bool>("--all", "List all plugins.");

        this.AddOption(listAllArgument);
        this.SetHandler(async (context) =>
        {
            var applicationOptions = OptionsUtils.GetValueForOption(
                new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), context);
            var listAll = OptionsUtils.GetValueForOption(listAllArgument, context);

            applicationOptions.InitializeLogger();
            using var root = await applicationOptions.CreateStdoutApplicationRootAsync();
            var query = "SELECT * FROM _plugins() WHERE 1=1";
            if (!listAll)
            {
                query += $@" AND (platform = _platform() OR platform = '{Application.PlatformMulti}');";
            }
            var result = await root.Thread.RunAsync(query, cancellationToken: context.GetCancellationToken());
            root.Thread.TopScope.Variables["result"] = result;
            await root.Thread.RunAsync("result");
        });
    }
}
#endif
