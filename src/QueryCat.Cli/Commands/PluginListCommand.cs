using System.CommandLine;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Storage;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginListCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginListCommand() : base("list", "List all available plugins.")
    {
        this.SetHandler(applicationOptions =>
        {
            applicationOptions.InitializeLogger();
            using var root = applicationOptions.CreateStdoutApplicationRoot();
            var result = root.Thread.RunFunction(InfoFunctions.Plugins);
            root.Thread.TopScope.DefineVariable("result", result);
            root.Thread.Run("result");
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption));
    }
}
#endif
