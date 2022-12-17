using System.CommandLine;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal class AstCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public AstCommand() : base("ast", "Show query AST for debugging.")
    {
        this.SetHandler((applicationOptions, query, files) =>
        {
            applicationOptions.InitializeLogger();
            var thread = applicationOptions.CreateExecutionThread();
            thread.BeforeStatementExecute += (_, threadArgs) =>
            {
                Console.WriteLine(thread.DumpAst());
                threadArgs.ContinueExecution = false;
            };
            RunQuery(thread, query, files);
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), QueryArgument, FilesOption);
    }
}
