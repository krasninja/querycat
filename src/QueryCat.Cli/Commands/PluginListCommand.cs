using System.CommandLine;
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
        this.SetHandler((applicationOptions, listAll) =>
        {
            applicationOptions.InitializeLogger();
            using var root = applicationOptions.CreateStdoutApplicationRoot();
            var query = "SELECT * FROM _plugins() WHERE 1=1";
            if (!listAll)
            {
                query += " AND platform = _platform();";
            }
            var result = root.Thread.Run(query);
            root.Thread.TopScope.Variables["result"] = result;
            root.Thread.Run("result");
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), listAllArgument);
    }
}
#endif
