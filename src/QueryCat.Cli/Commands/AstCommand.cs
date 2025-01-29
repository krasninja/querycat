using System.CommandLine;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal class AstCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public AstCommand() : base("ast", "Show query AST for debugging.")
    {
        this.SetHandler(async (context) =>
        {
            var applicationOptions = OptionsUtils.GetValueForOption(
                new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), context);
            var query = OptionsUtils.GetValueForOption(QueryArgument, context);
            var variables = OptionsUtils.GetValueForOption(VariablesOption, context);
            var files = OptionsUtils.GetValueForOption(FilesOption, context);

            applicationOptions.InitializeLogger();
            var root = await applicationOptions.CreateApplicationRootAsync();
            root.Thread.StatementExecuting += (_, threadArgs) =>
            {
                Console.WriteLine(root.Thread.DumpAst(threadArgs));
                threadArgs.ContinueExecution = false;
            };
            AddVariables(root.Thread, variables);
            await RunQueryAsync(root.Thread, query, files);
        });
    }
}
