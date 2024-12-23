using System.CommandLine;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal class AstCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public AstCommand() : base("ast", "Show query AST for debugging.")
    {
        this.SetHandler(async (applicationOptions, query, variables, files) =>
        {
            applicationOptions.InitializeLogger();
            var root = applicationOptions.CreateApplicationRoot();
            root.Thread.StatementExecuting += (_, threadArgs) =>
            {
                Console.WriteLine(root.Thread.DumpAst(threadArgs));
                threadArgs.ContinueExecution = false;
            };
            AddVariables(root.Thread, variables);
            await RunQueryAsync(root.Thread, query, files);
        },
            new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption),
            QueryArgument,
            VariablesOption,
            FilesOption);
    }
}
